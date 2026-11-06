using System.Security.Cryptography;
using System.Text;
using NexusPay.Domain.Common;
using NexusPay.Domain.Customers;

namespace NexusPay.Domain.Operations;

public sealed class FinancialOperation
{
    private readonly List<OperationStatusChange> _statusHistory = [];
    private readonly List<PaymentAttempt> _paymentAttempts = [];
    private FinancialOperation() { }

    private FinancialOperation(Guid id, string code, Guid customerId, OperationType type, Money money,
        string? description, string idempotencyKey, string requestHash, DateTimeOffset nowUtc)
    {
        Id = id; OperationCode = code; CustomerId = customerId; Type = type; Money = money;
        Description = description; IdempotencyKey = idempotencyKey; RequestHash = requestHash;
        Status = OperationStatus.Pending; CreatedAtUtc = nowUtc.ToUniversalTime();
        _statusHistory.Add(new OperationStatusChange(Id, null, Status, CreatedAtUtc, "Created", null, null));
    }

    public Guid Id { get; private set; }
    public string OperationCode { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public OperationType Type { get; private set; }
    public Money Money { get; private set; } = null!;
    public string? Description { get; private set; }
    public OperationStatus Status { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public string? LastProcessIdempotencyKey { get; private set; }
    public string? LastProviderCode { get; private set; }
    public string? LastProviderReference { get; private set; }
    public string? LastFailureCode { get; private set; }
    public int ProcessingAttempts { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }
    public IReadOnlyCollection<OperationStatusChange> StatusHistory => _statusHistory.AsReadOnly();
    public IReadOnlyCollection<PaymentAttempt> PaymentAttempts => _paymentAttempts.AsReadOnly();

    public static FinancialOperation Create(string operationCode, Customer customer, OperationType type,
        Money money, string? description, string idempotencyKey, DateTimeOffset nowUtc)
    {
        if (!customer.IsActive) throw new DomainRuleException("Operations cannot be created for an inactive customer.");
        var key = NormalizeKey(idempotencyKey, nameof(idempotencyKey));
        var code = string.IsNullOrWhiteSpace(operationCode) ? throw new DomainRuleException("Operation code is required.") : operationCode.Trim().ToUpperInvariant();
        var normalizedDescription = description?.Trim();
        if (normalizedDescription?.Length > 500) throw new DomainRuleException("Description must be at most 500 characters.");
        var payload = $"{customer.Id:N}|{type}|{money.Amount:0.00}|{money.Currency}|{normalizedDescription}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        return new FinancialOperation(Guid.NewGuid(), code, customer.Id, type, money, normalizedDescription, key, hash, nowUtc);
    }

    public bool HasSameRequest(string requestHash) => string.Equals(RequestHash, requestHash, StringComparison.Ordinal);

    public void StartProcessing(string processIdempotencyKey, Guid providerId, string providerCode,
        DateTimeOffset nowUtc, int maxAttempts = 3, string? actorUserId = null, string? correlationId = null)
    {
        if (Status is not (OperationStatus.Pending or OperationStatus.Failed))
            throw new DomainRuleException($"An operation in {Status} cannot be processed.");
        if (maxAttempts < 1 || ProcessingAttempts >= maxAttempts)
            throw new DomainRuleException("The maximum number of processing attempts has been reached.");
        var key = NormalizeKey(processIdempotencyKey, nameof(processIdempotencyKey));
        if (_paymentAttempts.Any(attempt => string.Equals(attempt.IdempotencyKey, key, StringComparison.Ordinal)))
            throw new ConflictException("A processing attempt with this idempotency key already exists.");
        if (providerId == Guid.Empty || string.IsNullOrWhiteSpace(providerCode) || providerCode.Trim().Length > 64)
            throw new DomainRuleException("Provider code is required and must be at most 64 characters.");
        LastProcessIdempotencyKey = key;
        LastProviderCode = providerCode.Trim().ToUpperInvariant();
        LastProviderReference = null;
        LastFailureCode = null;
        ProcessingAttempts++;
        _paymentAttempts.Add(new PaymentAttempt(Id, providerId, ProcessingAttempts, key, LastProviderCode, nowUtc));
        Transition(OperationStatus.Processing, nowUtc, "Processing started", actorUserId, correlationId);
    }

    public void Complete(string? providerReference, DateTimeOffset nowUtc, string? actorUserId = null, string? correlationId = null)
    {
        EnsureProcessing();
        LastProviderReference = providerReference?.Trim();
        _paymentAttempts[^1].Succeed(LastProviderReference, nowUtc);
        ProcessedAtUtc = nowUtc.ToUniversalTime();
        Transition(OperationStatus.Completed, nowUtc, "Simulation approved", actorUserId, correlationId);
    }

    public void Fail(string failureCode, DateTimeOffset nowUtc, PaymentAttemptStatus attemptStatus = PaymentAttemptStatus.Failed,
        string? actorUserId = null, string? correlationId = null)
    {
        EnsureProcessing();
        if (string.IsNullOrWhiteSpace(failureCode) || failureCode.Length > 80)
            throw new DomainRuleException("A failure code of at most 80 characters is required.");
        LastFailureCode = failureCode.Trim();
        if (attemptStatus is not (PaymentAttemptStatus.Rejected or PaymentAttemptStatus.Failed or PaymentAttemptStatus.TimedOut))
            throw new DomainRuleException("The payment attempt failure status is invalid.");
        _paymentAttempts[^1].Fail(LastFailureCode, attemptStatus, nowUtc);
        ProcessedAtUtc = nowUtc.ToUniversalTime();
        Transition(OperationStatus.Failed, nowUtc, LastFailureCode, actorUserId, correlationId);
    }

    public void Cancel(DateTimeOffset nowUtc, string? actorUserId = null, string? correlationId = null)
    {
        if (Status != OperationStatus.Pending) throw new DomainRuleException("Only pending operations can be cancelled.");
        Transition(OperationStatus.Cancelled, nowUtc, "Cancelled", actorUserId, correlationId);
    }

    private void EnsureProcessing()
    {
        if (Status != OperationStatus.Processing) throw new DomainRuleException("Only processing operations can receive a result.");
    }

    private void Transition(OperationStatus next, DateTimeOffset nowUtc, string? reason,
        string? actorUserId = null, string? correlationId = null)
    {
        var previous = Status;
        Status = next;
        UpdatedAtUtc = nowUtc.ToUniversalTime();
        _statusHistory.Add(new OperationStatusChange(Id, previous, next, UpdatedAtUtc.Value, reason,
            actorUserId?.Trim(), correlationId?.Trim()));
    }

    private static string NormalizeKey(string value, string field)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > 128)
            throw new DomainRuleException($"{field} is required and must be at most 128 characters.");
        return normalized.ToUpperInvariant();
    }
}
