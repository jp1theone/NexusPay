using System.Text.RegularExpressions;

namespace NexusPay.Domain.Common;

public static partial class SensitiveDataRedactor
{
    [GeneratedRegex("(?i)(?<key>authorization|password|token|secret|api[\\s_-]?key|connectionstring|cardnumber|pan|accountnumber|email|documentnumber)\\s*[:=]\\s*(?:\\\"[^\\\"]*\\\"|'[^']*'|[^\\r\\n,;]+)", RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveFieldPattern();

    public static string? Redact(string? text) => string.IsNullOrWhiteSpace(text)
        ? text
        : SensitiveFieldPattern().Replace(text, match => $"{match.Groups["key"].Value}=[REDACTED]");
}
