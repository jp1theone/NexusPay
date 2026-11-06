using NexusPay.Application.Auditing;
using NexusPay.Application.Common;
using NexusPay.Application.Customers;
using NexusPay.Domain.Auditing;
using NexusPay.Domain.Common;
using NexusPay.Domain.Operations;

namespace NexusPay.Application.Operations;

public sealed class SearchFinancialOperationsUseCase(IFinancialOperationRepository operations)
{
    public async Task<PageResult<FinancialOperationDto>> ExecuteAsync(Guid? customerId, OperationStatus? status,
        string? currency, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize,
        string sortBy, string sortDirection, CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100) throw new DomainRuleException("Page must be positive and page size must be between 1 and 100.");
        if (from.HasValue && to.HasValue && from.Value > to.Value) throw new DomainRuleException("The from date must not be later than the to date.");
        if (to.HasValue && from.HasValue && to.Value - from.Value > TimeSpan.FromDays(366))
            throw new DomainRuleException("The requested operation date range cannot exceed 366 days.");
        if (!new[] { "createdAt", "amount", "status" }.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            throw new DomainRuleException("The requested operation sort field is not supported.");
        if (!new[] { "asc", "desc" }.Contains(sortDirection, StringComparer.OrdinalIgnoreCase))
            throw new DomainRuleException("Sort direction must be asc or desc.");
        if (!string.IsNullOrWhiteSpace(currency) && !new[] { "PEN", "USD", "EUR" }.Contains(currency.Trim(), StringComparer.OrdinalIgnoreCase))
            throw new DomainRuleException("Currency must be PEN, USD or EUR.");
        var result = await operations.SearchAsync(customerId, status, currency, from?.ToUniversalTime(),
            to?.ToUniversalTime(), page, pageSize, sortBy, sortDirection, cancellationToken);
        return new PageResult<FinancialOperationDto>(result.Items.Select(FinancialOperationMapper.ToDto).ToArray(), result.TotalCount, page, pageSize);
    }
}

public sealed class GetFinancialOperationUseCase(IFinancialOperationRepository operations)
{
    public async Task<FinancialOperationDetailsDto> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var operation = await operations.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Operation was not found.");
        var history = operation.StatusHistory.OrderBy(entry => entry.ChangedAtUtc)
            .Select(entry => new OperationStatusChangeDto(entry.FromStatus, entry.ToStatus, entry.ChangedAtUtc,
                entry.Reason, entry.ActorUserId, entry.CorrelationId)).ToArray();
        var attempts = operation.PaymentAttempts.OrderBy(entry => entry.AttemptNumber)
            .Select(entry => new PaymentAttemptDto(entry.AttemptNumber, entry.ProviderCode, entry.IdempotencyKey,
                entry.Status, entry.ProviderReference, entry.FailureCode, entry.StartedAtUtc, entry.CompletedAtUtc)).ToArray();
        return new FinancialOperationDetailsDto(FinancialOperationMapper.ToDto(operation), history, attempts);
    }
}

public sealed class CancelFinancialOperationUseCase(IFinancialOperationRepository operations,
    IAuditLogRepository auditLogs, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task ExecuteAsync(Guid id, string? actorUserId, string? correlationId,
        CancellationToken cancellationToken = default)
    {
        var operation = await operations.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Operation was not found.");
        operation.Cancel(clock.UtcNow, actorUserId, correlationId);
        await auditLogs.AddAsync(AuditLog.Create(actorUserId, "FinancialOperationCancelled", "FinancialOperation",
            id.ToString("D"), clock.UtcNow, correlationId ?? Guid.NewGuid().ToString("N"), "Succeeded"), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
