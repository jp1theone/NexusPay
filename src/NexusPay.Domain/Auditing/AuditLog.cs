using NexusPay.Domain.Common;

namespace NexusPay.Domain.Auditing;

public sealed class AuditLog
{
    private AuditLog() { }

    private AuditLog(string? actorUserId, string action, string entityType, string entityId,
        DateTimeOffset occurredAtUtc, string? correlationId, string result, string? changeSummary)
    {
        ActorUserId = actorUserId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        OccurredAtUtc = occurredAtUtc.ToUniversalTime();
        CorrelationId = correlationId;
        Result = result;
        ChangeSummary = SensitiveDataRedactor.Redact(changeSummary);
    }

    public long Id { get; private set; }
    public string? ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string? CorrelationId { get; private set; }
    public string Result { get; private set; } = string.Empty;
    public string? ChangeSummary { get; private set; }

    public static AuditLog Create(string? actorUserId, string action, string entityType, string entityId,
        DateTimeOffset occurredAtUtc, string? correlationId, string result, string? changeSummary = null)
    {
        if (string.IsNullOrWhiteSpace(action) || action.Length > 80) throw new DomainRuleException("Audit action is required and must be at most 80 characters.");
        if (string.IsNullOrWhiteSpace(entityType) || entityType.Length > 80) throw new DomainRuleException("Audit entity type is required and must be at most 80 characters.");
        if (string.IsNullOrWhiteSpace(entityId) || entityId.Length > 128) throw new DomainRuleException("Audit entity id is required and must be at most 128 characters.");
        if (string.IsNullOrWhiteSpace(result) || result.Length > 32) throw new DomainRuleException("Audit result is required and must be at most 32 characters.");
        if (actorUserId?.Length > 450 || correlationId?.Length > 128 || changeSummary?.Length > 1000)
            throw new DomainRuleException("An audit field exceeds its maximum length.");
        return new AuditLog(actorUserId, action.Trim(), entityType.Trim(), entityId.Trim(), occurredAtUtc,
            correlationId?.Trim(), result.Trim(), changeSummary?.Trim());
    }
}
