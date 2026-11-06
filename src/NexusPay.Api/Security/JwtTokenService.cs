using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using NexusPay.Infrastructure.Identity;

namespace NexusPay.Api.Security;

public sealed record IssuedAccessToken(string AccessToken, DateTimeOffset ExpiresAtUtc);

public interface IJwtTokenService
{
    Task<IssuedAccessToken> CreateAsync(NexusPayUser user, CancellationToken cancellationToken = default);
}

public sealed class JwtTokenService(IConfiguration configuration, UserManager<NexusPayUser> users) : IJwtTokenService
{
    public async Task<IssuedAccessToken> CreateAsync(NexusPayUser user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var roles = await users.GetRolesAsync(user);
        var now = DateTimeOffset.UtcNow;
        var expiry = now.AddMinutes(configuration.GetValue("Jwt:ExpiryMinutes", 15));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var keyBytes = Convert.FromBase64String(configuration["Jwt:SigningKey"]!);
        var credentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(configuration["Jwt:Issuer"], configuration["Jwt:Audience"],
            claims, now.UtcDateTime, expiry.UtcDateTime, credentials);
        return new IssuedAccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiry);
    }
}
