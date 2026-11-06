using NexusPay.Domain.Common;

namespace NexusPay.Domain.Providers;

public sealed class PaymentProvider
{
    private PaymentProvider() { }

    private PaymentProvider(Guid id, string code, string name, DateTimeOffset nowUtc)
    {
        if (id == Guid.Empty) throw new DomainRuleException("Provider id is required.");
        Id = id;
        ProviderCode = code.Trim().ToUpperInvariant();
        Name = Required(name, 120, "Provider name");
        IsEnabled = true;
        IsAvailable = true;
        CreatedAtUtc = nowUtc.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public string ProviderCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public PaymentProviderType Type { get; private set; } = PaymentProviderType.Simulator;
    public bool IsEnabled { get; private set; }
    public bool IsAvailable { get; private set; }
    public string? ConfigurationJson { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public static PaymentProvider Create(Guid id, string code, string name, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 64)
            throw new DomainRuleException("Provider code is required and must be at most 64 characters.");
        return new PaymentProvider(id, code, name, nowUtc);
    }

    public void SetAvailability(bool available, DateTimeOffset nowUtc)
    {
        IsAvailable = available;
        UpdatedAtUtc = nowUtc.ToUniversalTime();
    }

    public void Configure(string name, bool enabled, bool available, DateTimeOffset nowUtc)
    {
        Name = Required(name, 120, "Provider name");
        IsEnabled = enabled;
        IsAvailable = enabled && available;
        UpdatedAtUtc = nowUtc.ToUniversalTime();
    }

    public void Disable(DateTimeOffset nowUtc)
    {
        IsEnabled = false;
        IsAvailable = false;
        UpdatedAtUtc = nowUtc.ToUniversalTime();
    }

    private static string Required(string value, int maxLength, string field)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maxLength)
            throw new DomainRuleException($"{field} is required and must be at most {maxLength} characters.");
        return normalized;
    }
}
