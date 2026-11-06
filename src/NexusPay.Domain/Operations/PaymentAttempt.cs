namespace NexusPay.Domain.Operations;

public sealed class PaymentAttempt
{
    private PaymentAttempt() { }

    internal PaymentAttempt(Guid operationId, Guid providerId, int attemptNumber, string idempotencyKey,
        string providerCode, DateTimeOffset startedAtUtc)
    {
        Id = Guid.NewGuid();
        OperationId = operationId;
        PaymentProviderId = providerId;
        AttemptNumber = attemptNumber;
        IdempotencyKey = idempotencyKey;
        ProviderCode = providerCode;
        Status = PaymentAttemptStatus.Processing;
        StartedAtUtc = startedAtUtc.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public Guid OperationId { get; private set; }
    public Guid PaymentProviderId { get; private set; }
    public int AttemptNumber { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string ProviderCode { get; private set; } = string.Empty;
    public PaymentAttemptStatus Status { get; private set; }
    public string? ProviderReference { get; private set; }
    public string? FailureCode { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    internal void Succeed(string? providerReference, DateTimeOffset completedAtUtc)
    {
        Status = PaymentAttemptStatus.Succeeded;
        ProviderReference = providerReference?.Trim();
        CompletedAtUtc = completedAtUtc.ToUniversalTime();
    }

    internal void Fail(string failureCode, PaymentAttemptStatus status, DateTimeOffset completedAtUtc)
    {
        Status = status;
        FailureCode = failureCode.Trim();
        CompletedAtUtc = completedAtUtc.ToUniversalTime();
    }
}
