using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusPay.Application.Auth;
using NexusPay.Infrastructure.Identity;
using NexusPay.Api.Middleware;
using NexusPay.Application.Auditing;
using NexusPay.Application.Common;
using NexusPay.Domain.Auditing;
using NexusPay.Infrastructure.Persistence;

namespace NexusPay.Api.Controllers;

public sealed record CreateUserRequest(string Email, string Password, IReadOnlyList<string> Roles);
public sealed record SetUserRolesRequest(IReadOnlyList<string> Roles);
public sealed record SetUserStatusRequest(bool IsActive);
public sealed record UserDto(string Id, string Email, bool IsActive, DateTimeOffset CreatedAtUtc, IReadOnlyList<string> Roles);
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

[ApiController]
[Route("api/users")]
[Authorize(Roles = RoleNames.Administrator)]
public sealed class UsersController(UserManager<NexusPayUser> users, RoleManager<IdentityRole> roles,
    NexusPayDbContext context, IAuditLogRepository auditLogs, IClock clock) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<UserDto>>> List([FromQuery] string? search = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize is < 1 or > 100) return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid pagination values" });
        var query = users.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(user => user.Email != null && user.Email.Contains(term));
        }
        var total = await query.CountAsync();
        var pageUsers = await query.OrderBy(user => user.Email).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var items = new List<UserDto>(pageUsers.Count);
        foreach (var user in pageUsers)
            items.Add(new UserDto(user.Id, user.Email ?? string.Empty, user.IsActive, user.CreatedAtUtc, (await users.GetRolesAsync(user)).ToArray()));
        return Ok(new PagedResponse<UserDto>(items, total, page, pageSize));
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 254 ||
            !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.Email))
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [nameof(request.Email)] = ["A valid email is required."] }));
        if (request.Roles is null || request.Roles.Count == 0 || request.Roles.Any(role => !IsAllowedRole(role)) ||
            request.Roles.Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Roles.Count)
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [nameof(request.Roles)] = ["Choose one or more valid NexusPay roles."] }));

        var requestedRoles = request.Roles.Select(NormalizeRole).ToArray();
        foreach (var role in requestedRoles)
            if (!await roles.RoleExistsAsync(role))
                return Conflict(new ProblemDetails { Status = 409, Title = "A requested role is not configured" });

        var email = request.Email.Trim();
        return await context.Database.CreateExecutionStrategy().ExecuteAsync<ActionResult<UserDto>>(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(HttpContext.RequestAborted);
            var user = new NexusPayUser { UserName = email, Email = email, IsActive = true, CreatedAtUtc = clock.UtcNow };
            var created = await users.CreateAsync(user, request.Password);
            if (!created.Succeeded) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [nameof(request.Password)] = created.Errors.Select(error => error.Description).ToArray() }));
            foreach (var role in requestedRoles)
            {
                var assigned = await users.AddToRoleAsync(user, role);
                if (!assigned.Succeeded) return Conflict(new ProblemDetails { Status = 409, Title = "Could not assign the requested roles" });
            }
            await WriteAuditAsync("user.created", user.Id, $"Account created with roles: {string.Join(",", requestedRoles)}.");
            await transaction.CommitAsync(HttpContext.RequestAborted);
            return CreatedAtAction(nameof(List), new { search = email }, new UserDto(user.Id, email, user.IsActive,
                user.CreatedAtUtc, requestedRoles));
        });
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> SetStatus(string id, SetUserStatusRequest request)
    {
        return await context.Database.CreateExecutionStrategy().ExecuteAsync<IActionResult>(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(HttpContext.RequestAborted);
            var user = await users.FindByIdAsync(id);
            if (user is null) return NotFound();
            var currentRoles = await users.GetRolesAsync(user);
            if (!request.IsActive && user.IsActive && currentRoles.Contains(RoleNames.Administrator, StringComparer.OrdinalIgnoreCase) &&
                await ActiveAdministratorCountAsync() <= 1)
                return Conflict(new ProblemDetails { Status = 409, Title = "The last active administrator cannot be disabled" });
            user.IsActive = request.IsActive;
            if (!request.IsActive) user.SecurityStamp = Guid.NewGuid().ToString("N");
            var result = await users.UpdateAsync(user);
            if (!result.Succeeded) return Conflict(new ProblemDetails { Status = 409, Title = "Could not update user status" });
            await WriteAuditAsync("user.status_changed", user.Id, $"Account active={user.IsActive}.");
            await transaction.CommitAsync(HttpContext.RequestAborted);
            return NoContent();
        });
    }

    [HttpPut("{id}/roles")]
    public async Task<IActionResult> SetRoles(string id, SetUserRolesRequest request)
    {
        if (request.Roles is null || request.Roles.Count == 0 || request.Roles.Any(role => !IsAllowedRole(role)) ||
            request.Roles.Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Roles.Count)
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [nameof(request.Roles)] = ["Choose one or more valid NexusPay roles."] }));
        var requested = request.Roles.Select(NormalizeRole).ToArray();
        foreach (var role in requested)
            if (!await roles.RoleExistsAsync(role)) return Conflict(new ProblemDetails { Status = 409, Title = "A requested role is not configured" });
        return await context.Database.CreateExecutionStrategy().ExecuteAsync<IActionResult>(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(HttpContext.RequestAborted);
            var user = await users.FindByIdAsync(id);
            if (user is null) return NotFound();
            var current = await users.GetRolesAsync(user);
            if (user.IsActive && current.Contains(RoleNames.Administrator, StringComparer.OrdinalIgnoreCase) &&
                !requested.Contains(RoleNames.Administrator, StringComparer.OrdinalIgnoreCase) &&
                await ActiveAdministratorCountAsync() <= 1)
                return Conflict(new ProblemDetails { Status = 409, Title = "The last active administrator cannot lose the administrator role" });
            var removed = await users.RemoveFromRolesAsync(user, current);
            if (!removed.Succeeded) return Conflict(new ProblemDetails { Status = 409, Title = "Could not replace user roles" });
            foreach (var role in requested)
            {
                var assigned = await users.AddToRoleAsync(user, role);
                if (!assigned.Succeeded) return Conflict(new ProblemDetails { Status = 409, Title = "Could not assign the requested roles" });
            }
            await WriteAuditAsync("user.roles_changed", user.Id, $"Roles set: {string.Join(",", requested)}.");
            await transaction.CommitAsync(HttpContext.RequestAborted);
            return NoContent();
        });
    }

    private async Task WriteAuditAsync(string action, string userId, string summary)
    {
        await auditLogs.AddAsync(AuditLog.Create(User.FindFirstValue(ClaimTypes.NameIdentifier), action,
            "User", userId, clock.UtcNow, HttpContext.Items[CorrelationIdMiddleware.ItemName]?.ToString(),
            "Success", summary), HttpContext.RequestAborted);
        await context.SaveChangesAsync(HttpContext.RequestAborted);
    }

    private static readonly string[] AllowedRoles = [RoleNames.Administrator, RoleNames.Operator, RoleNames.Auditor];
    private static bool IsAllowedRole(string value) => AllowedRoles.Contains(value, StringComparer.OrdinalIgnoreCase);
    private static string NormalizeRole(string value) => AllowedRoles.Single(role => string.Equals(role, value, StringComparison.OrdinalIgnoreCase));

    private async Task<int> ActiveAdministratorCountAsync() =>
        (await users.GetUsersInRoleAsync(RoleNames.Administrator)).Count(user => user.IsActive);
}
