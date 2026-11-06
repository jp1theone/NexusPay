using NexusPay.Domain.Common;

namespace NexusPay.Domain.Operations;

public sealed record Money
{
    private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.Ordinal) { "PEN", "USD", "EUR" };

    private Money(decimal amount, string currency) { Amount = amount; Currency = currency; }

    public decimal Amount { get; }
    public string Currency { get; }

    public static Money Create(decimal amount, string currency)
    {
        var normalizedCurrency = currency?.Trim().ToUpperInvariant();
        if (amount <= 0 || amount > 9_999_999_999_999_999.99m || decimal.Round(amount, 2) != amount)
            throw new DomainRuleException("Amount must be positive, fit decimal(18,2), and have at most two decimal places.");
        if (normalizedCurrency is null || !SupportedCurrencies.Contains(normalizedCurrency))
            throw new DomainRuleException("Currency must be one of PEN, USD or EUR.");
        return new Money(amount, normalizedCurrency);
    }
}
