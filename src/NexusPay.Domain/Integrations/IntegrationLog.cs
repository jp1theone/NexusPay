using NexusPay.Domain.Common;

namespace NexusPay.Domain.Integrations;

public sealed class IntegrationLog
{
    private IntegrationLog() { }

    private IntegrationLog(Guid providerId, Guid? paymentAttemptId, string correlationId,
        IntegrationOutcome outcome, string? errorCode, int durationMilliseconds,
        DateTimeOffset occurredAtUtc, string? technicalDetails)
    {
        PaymentProviderId = providerId;
        PaymentAttemptId = paymentAttemptId;
        CorrelationId = correlationId;
        Outcome = outcome;
        ErrorCode = errorCode;
        DurationMilliseconds = durationMilliseconds;
        OccurredAtUtc = occurredAtUtc.ToUniversalTime();
        TechnicalDetails = SensitiveDataRedactor.Redact(technicalDetails);
    }

    public long Id { get; private set; }
    public Guid PaymentProviderId { get; private set; }
    public Guid? PaymentAttemptId { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public IntegrationOutcome Outcome { get; private set; }
    public string? ErrorCode { get; private set; }
    public int DurationMilliseconds { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string? TechnicalDetails { get; private set; }

    public static IntegrationLog Create(Guid providerId, Guid? paymentAttemptId, string correlationId,
        IntegrationOutcome outcome, string? errorCode, int durationMilliseconds,
        DateTimeOffset occurredAtUtc, string? technicalDetails = null)
    {
        if (providerId == Guid.Empty) throw new DomainRuleException("Integration provider id is required.");
        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 128)
            throw new DomainRuleException("Correlation id is required and must be at most 128 characters.");
        if (durationMilliseconds < 0) throw new DomainRuleException("Integration duration cannot be negative.");
        if (errorCode?.Length > 80 || technicalDetails?.Length > 2000)
            throw new DomainRuleException("An integration log field exceeds its maximum length.");
        return new IntegrationLog(providerId, paymentAttemptId, correlationId.Trim(), outcome,
            errorCode?.Trim(), durationMilliseconds, occurredAtUtc, technicalDetails?.Trim());
    }
}
