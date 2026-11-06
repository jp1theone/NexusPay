using NexusPay.Domain.Auditing;
using NexusPay.Domain.Integrations;
using NexusPay.Domain.Providers;

namespace NexusPay.UnitTests.Domain;

public sealed class ProviderAndAuditTests
{
    [Fact]
    public void Provider_NormalizesCodeAndDisableMakesItUnavailable()
    {
        var now = DateTimeOffset.UtcNow;
        var provider = PaymentProvider.Create(Guid.NewGuid(), " nexusProviderAlpha ", " Alpha ", now);

        provider.Disable(now.AddMinutes(1));

        Assert.Equal("NEXUSPROVIDERALPHA", provider.ProviderCode);
        Assert.False(provider.IsEnabled);
        Assert.False(provider.IsAvailable);
    }

    [Fact]
    public void AuditAndIntegrationLogs_RedactSensitiveKeyValueData()
    {
        var now = DateTimeOffset.UtcNow;
        var audit = AuditLog.Create("operator-1", "Update", "Customer", "customer-1",
            now, "corr-1", "Succeeded", "email=ada@example.test; changed status");
        var integration = IntegrationLog.Create(Guid.NewGuid(), null, "corr-2",
            IntegrationOutcome.Failed, "REMOTE_ERROR", 20, now,
            "Authorization=Bearer sample-token; password=sample-password");

        Assert.DoesNotContain("ada@example.test", audit.ChangeSummary);
        Assert.DoesNotContain("sample-token", integration.TechnicalDetails);
        Assert.DoesNotContain("sample-password", integration.TechnicalDetails);
        Assert.Contains("[REDACTED]", integration.TechnicalDetails);
    }
}
