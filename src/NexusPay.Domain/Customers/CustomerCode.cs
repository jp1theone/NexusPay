using System.Text.RegularExpressions;
using NexusPay.Domain.Common;

namespace NexusPay.Domain.Customers;

public sealed record CustomerCode
{
    private static readonly Regex Format = new(@"^[A-Z0-9][A-Z0-9-]{2,31}$", RegexOptions.CultureInvariant);

    private CustomerCode(string value) => Value = value;

    public string Value { get; }
    public string NormalizedValue => Value.ToUpperInvariant();

    public static CustomerCode Create(string value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || !Format.IsMatch(normalized))
        {
            throw new DomainRuleException("Customer code must contain 3 to 32 letters, digits or hyphens.");
        }

        return new CustomerCode(normalized);
    }
}
