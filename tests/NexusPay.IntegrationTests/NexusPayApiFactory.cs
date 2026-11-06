using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace NexusPay.IntegrationTests;

public sealed class NexusPayApiFactory : WebApplicationFactory<Program>
{
    public string AdminEmail { get; }
    public string AdminPassword { get; }

    public NexusPayApiFactory()
    {
        var secrets = new ConfigurationBuilder().AddUserSecrets(typeof(Program).Assembly).Build();
        AdminEmail = Environment.GetEnvironmentVariable("NEXUSPAY_TEST_ADMIN_EMAIL") ??
            secrets["BootstrapAdmin:Email"] ?? throw new InvalidOperationException("Configure NEXUSPAY_TEST_ADMIN_EMAIL for SQL Server integration tests.");
        AdminPassword = Environment.GetEnvironmentVariable("NEXUSPAY_TEST_ADMIN_PASSWORD") ??
            secrets["BootstrapAdmin:Password"] ?? throw new InvalidOperationException("Configure NEXUSPAY_TEST_ADMIN_PASSWORD for SQL Server integration tests.");
        var connectionString = Environment.GetEnvironmentVariable("NEXUSPAY_TEST_CONNECTION_STRING") ??
            secrets.GetConnectionString("NexusPayIntegrationTests") ??
            throw new InvalidOperationException("Configure ConnectionStrings:NexusPayIntegrationTests in User Secrets for SQL Server integration tests.");
        if (!connectionString.Contains("NexusPay_IntegrationTests", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Integration tests require the isolated NexusPay_IntegrationTests database.");
        ConnectionString = connectionString;

        // WebApplicationFactory's IConfiguration callback runs too late for Program's
        // top-level service registration. Seed environment configuration before the host starts.
        Environment.SetEnvironmentVariable("ConnectionStrings__NexusPay", ConnectionString);
        Environment.SetEnvironmentVariable("Database__ApplyMigrationsOnStartup", "true");
        Environment.SetEnvironmentVariable("RateLimiting__AuthPermitLimit", "100");
    }

    public string ConnectionString { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:NexusPay"] = ConnectionString,
                ["Database:ApplyMigrationsOnStartup"] = "true",
                ["RateLimiting:AuthPermitLimit"] = "100"
            }));
    }
}
