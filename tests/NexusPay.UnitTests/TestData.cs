using NexusPay.Domain.Operations;

namespace NexusPay.UnitTests;

internal static class TestData
{
    public static NexusPay.Domain.Customers.Customer Customer() => NexusPay.Domain.Customers.Customer.Create(
        "CST-001", "Ada", "Lovelace", "ada@example.test", null, null, null,
        DateTimeOffset.Parse("2026-01-02T03:04:05Z"));

    public static FinancialOperation Operation(DateTimeOffset nowUtc) => FinancialOperation.Create("OP-001",
        Customer(), OperationType.Payment, Money.Create(12.50m, "PEN"), "test", "operation-key", nowUtc);
}
