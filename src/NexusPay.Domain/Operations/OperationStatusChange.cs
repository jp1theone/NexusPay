namespace NexusPay.Domain.Operations;

public sealed class OperationStatusChange
{
    private OperationStatusChange() { }

    internal OperationStatusChange(Guid operationId, OperationStatus? fromStatus, OperationStatus toStatus,
        DateTimeOffset changedAtUtc, string? reason, string? actorUserId, string? correlationId)
    {
        OperationId = operationId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedAtUtc = changedAtUtc.ToUniversalTime();
        Reason = reason;
        ActorUserId = actorUserId;
        CorrelationId = correlationId;
    }

    public long Id { get; private set; }
    public Guid OperationId { get; private set; }
    public OperationStatus? FromStatus { get; private set; }
    public OperationStatus ToStatus { get; private set; }
    public DateTimeOffset ChangedAtUtc { get; private set; }
    public string? Reason { get; private set; }
    public string? ActorUserId { get; private set; }
    public string? CorrelationId { get; private set; }
}
