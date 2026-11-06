using NexusPay.Domain.Operations;
using NexusPay.Domain.Providers;
using NexusPay.Domain.Auditing;

namespace NexusPay.Application.Operations;

public sealed record CreateFinancialOperationRequest(Guid CustomerId, OperationType Type,
    decimal Amount, string Currency, string? Description, string IdempotencyKey);

public sealed record ProcessFinancialOperationRequest(string IdempotencyKey);

public sealed record FinancialOperationDto(Guid Id, string OperationCode, Guid CustomerId,
    OperationType Type, decimal Amount, string Currency, OperationStatus Status,
    string? Description, int ProcessingAttempts, string? ProviderCode,
    string? ProviderReference, string? FailureCode, DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ProcessedAtUtc);

public sealed record OperationStatusChangeDto(OperationStatus? FromStatus, OperationStatus ToStatus,
    DateTimeOffset ChangedAtUtc, string? Reason, string? ActorUserId, string? CorrelationId);

public sealed record PaymentAttemptDto(int AttemptNumber, string ProviderCode, string IdempotencyKey,
    PaymentAttemptStatus Status, string? ProviderReference, string? FailureCode,
    DateTimeOffset StartedAtUtc, DateTimeOffset? CompletedAtUtc);

public sealed record FinancialOperationDetailsDto(FinancialOperationDto Operation,
    IReadOnlyList<OperationStatusChangeDto> StatusHistory, IReadOnlyList<PaymentAttemptDto> PaymentAttempts);

public interface IFinancialOperationRepository
{
    Task<FinancialOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FinancialOperation?> GetByIdempotencyKeyAsync(string key, CancellationToken cancellationToken = default);
    Task AddAsync(FinancialOperation operation, CancellationToken cancellationToken = default);
    Task AddPaymentAttemptAsync(PaymentAttempt attempt, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<FinancialOperation> Items, int TotalCount)> SearchAsync(Guid? customerId,
        OperationStatus? status, string? currency, DateTimeOffset? from, DateTimeOffset? to,
        int page, int pageSize, string sortBy, string sortDirection, CancellationToken cancellationToken = default);
}

public interface IPaymentProviderRepository
{
    Task<PaymentProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentProvider?> GetByCodeAsync(string providerCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentProvider>> GetEnabledAsync(CancellationToken cancellationToken = default);
    Task AddAsync(PaymentProvider provider, CancellationToken cancellationToken = default);
}

public interface IOperationCodeGenerator
{
    string NextCode(DateTimeOffset nowUtc);
}

public enum PaymentSimulationOutcome { Approved, Rejected, TechnicalError, Timeout }

public sealed record PaymentSimulationResult(PaymentSimulationOutcome Outcome, string? ProviderReference = null,
    string? FailureCode = null);

public sealed record PaymentSimulationRequest(Guid OperationId, OperationType Type,
    decimal Amount, string Currency, string ProcessIdempotencyKey);

public interface IPaymentSimulator
{
    Guid ProviderId { get; }
    string ProviderCode { get; }
    Task<PaymentSimulationResult> ProcessAsync(PaymentSimulationRequest request,
        CancellationToken cancellationToken = default);
}
