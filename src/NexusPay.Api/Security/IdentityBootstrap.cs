using Microsoft.AspNetCore.Identity;
using NexusPay.Application.Auth;
using NexusPay.Infrastructure.Identity;

namespace NexusPay.Api.Security;

public static class IdentityBootstrap
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration,
        IHostEnvironment environment)
    {
        using var scope = services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var roleName in new[] { RoleNames.Administrator, RoleNames.Operator, RoleNames.Auditor })
        {
            if (!await roles.RoleExistsAsync(roleName))
            {
                var created = await roles.CreateAsync(new IdentityRole(roleName));
                if (!created.Succeeded) throw new InvalidOperationException("Could not initialize the NexusPay role catalog.");
            }
        }

        if (!environment.IsDevelopment()) return;
        var email = configuration["BootstrapAdmin:Email"];
        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

        var users = scope.ServiceProvider.GetRequiredService<UserManager<NexusPayUser>>();
        var administrator = await users.FindByEmailAsync(email.Trim());
        if (administrator is null)
        {
            administrator = new NexusPayUser
            {
                UserName = email.Trim(), Email = email.Trim(), IsActive = true, CreatedAtUtc = DateTimeOffset.UtcNow
            };
            var created = await users.CreateAsync(administrator, password);
            if (!created.Succeeded)
                throw new InvalidOperationException("Could not initialize the local administrator account: " +
                    string.Join("; ", created.Errors.Select(error => error.Code)));
        }

        if (!await users.IsInRoleAsync(administrator, RoleNames.Administrator))
        {
            var assigned = await users.AddToRoleAsync(administrator, RoleNames.Administrator);
            if (!assigned.Succeeded) throw new InvalidOperationException("Could not assign the local administrator role.");
        }
    }
}
