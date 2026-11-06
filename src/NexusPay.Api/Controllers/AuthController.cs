using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using NexusPay.Api.Security;
using NexusPay.Infrastructure.Identity;

namespace NexusPay.Api.Controllers;

public sealed record LoginRequest(string Email, string Password);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record AuthenticatedUserDto(string Id, string Email, IReadOnlyList<string> Roles);
public sealed record LoginResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAtUtc,
    AuthenticatedUserDto User);

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(request => request.Password).NotEmpty().MaximumLength(256);
    }
}

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<NexusPayUser> users,
    SignInManager<NexusPayUser> signInManager,
    IJwtTokenService tokenService,
    IValidator<LoginRequest> loginValidator) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive ||
            !(await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true)).Succeeded)
            return Unauthorized(new ProblemDetails { Status = 401, Title = "Invalid email or password", Extensions = { ["code"] = "invalid_credentials" } });

        var token = await tokenService.CreateAsync(user, cancellationToken);
        var roles = await users.GetRolesAsync(user);
        return Ok(new LoginResponse(token.AccessToken, "Bearer", token.ExpiresAtUtc,
            new AuthenticatedUserDto(user.Id, user.Email!, roles.ToArray())));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthenticatedUserDto>> Me()
    {
        var user = await users.GetUserAsync(User);
        if (user is null || !user.IsActive) return Unauthorized();
        var roles = await users.GetRolesAsync(user);
        return Ok(new AuthenticatedUserDto(user.Id, user.Email!, roles.ToArray()));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [nameof(request.NewPassword)] = ["Current and new passwords are required."] }));
        var user = await users.GetUserAsync(User);
        if (user is null || !user.IsActive) return Unauthorized();
        var result = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [nameof(request.NewPassword)] = result.Errors.Select(error => error.Description).ToArray() }));
        return NoContent();
    }
}
