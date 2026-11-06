using System.Text.RegularExpressions;
using NexusPay.Domain.Common;

namespace NexusPay.Domain.Customers;

public sealed record EmailAddress
{
    private static readonly Regex Format = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.CultureInvariant);

    private EmailAddress(string value) => Value = value;

    public string Value { get; }
    public string NormalizedValue => Value.ToUpperInvariant();

    public static EmailAddress Create(string value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > 254 || !Format.IsMatch(trimmed))
        {
            throw new DomainRuleException("The email address is invalid.");
        }

        return new EmailAddress(trimmed);
    }
}
