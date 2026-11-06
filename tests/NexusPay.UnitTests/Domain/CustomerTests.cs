using NexusPay.Domain.Common;
using NexusPay.Domain.Customers;
using NexusPay.UnitTests;

namespace NexusPay.UnitTests.Domain;

public sealed class CustomerTests
{
    [Fact]
    public void Create_NormalizesCodeAndAcceptsEmailWithoutCaseRestrictions()
    {
        var customer = Customer.Create(" nx-001 ", " Ada ", "Lovelace", "ADA@Example.com",
            null, null, null, DateTimeOffset.Parse("2026-01-02T03:04:05-05:00"));

        Assert.Equal("NX-001", customer.CustomerCode);
        Assert.Equal("ADA@Example.com", customer.Email.Value);
        Assert.Equal("ADA@EXAMPLE.COM", customer.Email.NormalizedValue);
        Assert.Equal(TimeSpan.Zero, customer.CreatedAtUtc.Offset);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("name @example.com")]
    public void Create_RejectsInvalidEmail(string email)
    {
        Assert.Throws<DomainRuleException>(() => EmailAddress.Create(email));
    }

    [Fact]
    public void Deactivate_IsLogicalAndIdempotent()
    {
        var customer = TestData.Customer();
        var changedAt = DateTimeOffset.UtcNow;

        customer.Deactivate(changedAt);
        customer.Deactivate(changedAt.AddMinutes(1));

        Assert.False(customer.IsActive);
        Assert.Equal(changedAt, customer.UpdatedAtUtc);
        Assert.Throws<DomainRuleException>(() => { customer.Update("Ada", "Lovelace", "a@b.com", null, null, null, changedAt); });
    }
}
