using NexusPay.Domain.Common;
using NexusPay.Domain.Operations;
using NexusPay.UnitTests;

namespace NexusPay.UnitTests.Domain;

public sealed class FinancialOperationTests
{
    [Theory]
    [InlineData(0, "PEN")]
    [InlineData(-1, "USD")]
    [InlineData(10.001, "PEN")]
    [InlineData(10, "GBP")]
    public void Money_RejectsInvalidAmountOrCurrency(decimal amount, string currency)
    {
        Assert.Throws<DomainRuleException>(() => Money.Create(amount, currency));
    }

    [Fact]
    public void Money_NormalizesSupportedCurrency()
    {
        Assert.Equal("USD", Money.Create(12.30m, " usd ").Currency);
    }

    [Fact]
    public void Operation_TracksValidTransitionsAndAllowsBoundedRetry()
    {
        var now = DateTimeOffset.UtcNow;
        var operation = TestData.Operation(now);
        var providerId = Guid.Parse("c6e7d319-868b-4a11-929d-a51d6d0d5e10");
        operation.StartProcessing("process-1", providerId, "nexusprovideralpha", now.AddSeconds(1));
        operation.Fail("SIMULATED_TIMEOUT", now.AddSeconds(2));
        operation.StartProcessing("process-2", providerId, "NexusProviderAlpha", now.AddSeconds(3), maxAttempts: 2);
        operation.Fail("SIMULATED_REJECTION", now.AddSeconds(4), PaymentAttemptStatus.Rejected);

        Assert.Throws<ConflictException>(() => { operation.StartProcessing("process-1", providerId, "provider", now.AddSeconds(5), maxAttempts: 3); });
        operation.StartProcessing("process-3", providerId, "NexusProviderAlpha", now.AddSeconds(5), maxAttempts: 3);
        operation.Complete("sim-ref-1", now.AddSeconds(6));

        Assert.Equal(OperationStatus.Completed, operation.Status);
        Assert.Equal(3, operation.ProcessingAttempts);
        Assert.Equal(7, operation.StatusHistory.Count);
        Assert.Equal(3, operation.PaymentAttempts.Count);
        Assert.Equal("NEXUSPROVIDERALPHA", operation.LastProviderCode);
        Assert.Throws<DomainRuleException>(() => { operation.StartProcessing("process-4", providerId, "provider", now.AddSeconds(5)); });
    }

    [Fact]
    public void Operation_RejectsReusingProcessingKeyAndCancellingAfterProcessing()
    {
        var now = DateTimeOffset.UtcNow;
        var operation = TestData.Operation(now);
        var providerId = Guid.Parse("c6e7d319-868b-4a11-929d-a51d6d0d5e10");
        operation.StartProcessing("same-key", providerId, "provider", now);
        operation.Fail("SIMULATED_TIMEOUT", now.AddSeconds(1));

        Assert.Throws<ConflictException>(() => { operation.StartProcessing("same-key", providerId, "provider", now.AddSeconds(2)); });
        Assert.Throws<DomainRuleException>(() => { operation.Cancel(now.AddSeconds(2)); });
    }

    [Fact]
    public void Operation_RejectsInactiveCustomer()
    {
        var customer = TestData.Customer();
        customer.Deactivate(DateTimeOffset.UtcNow);
        Assert.Throws<DomainRuleException>(() => FinancialOperation.Create("OP-1", customer,
            OperationType.Payment, Money.Create(1m, "PEN"), null, "key", DateTimeOffset.UtcNow));
    }
}
