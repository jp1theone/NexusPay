using Microsoft.EntityFrameworkCore;
using NexusPay.Application.Operations;
using NexusPay.Domain.Operations;

namespace NexusPay.Infrastructure.Persistence;

public sealed class FinancialOperationRepository(NexusPayDbContext context) : IFinancialOperationRepository
{
    public Task<FinancialOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.FinancialOperations.Include(operation => operation.StatusHistory)
            .Include(operation => operation.PaymentAttempts)
            .SingleOrDefaultAsync(operation => operation.Id == id, cancellationToken);

    public Task<FinancialOperation?> GetByIdempotencyKeyAsync(string key, CancellationToken cancellationToken = default) =>
        context.FinancialOperations.SingleOrDefaultAsync(operation => operation.IdempotencyKey == key, cancellationToken);

    public Task AddAsync(FinancialOperation operation, CancellationToken cancellationToken = default)
    {
        context.FinancialOperations.Add(operation);
        return Task.CompletedTask;
    }

    public Task AddPaymentAttemptAsync(NexusPay.Domain.Operations.PaymentAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        context.PaymentAttempts.Add(attempt);
        return Task.CompletedTask;
    }

    public async Task<(IReadOnlyList<FinancialOperation> Items, int TotalCount)> SearchAsync(Guid? customerId,
        OperationStatus? status, string? currency, DateTimeOffset? from, DateTimeOffset? to,
        int page, int pageSize, string sortBy, string sortDirection, CancellationToken cancellationToken = default)
    {
        var query = context.FinancialOperations.AsNoTracking().AsQueryable();
        if (customerId.HasValue) query = query.Where(operation => operation.CustomerId == customerId.Value);
        if (status.HasValue) query = query.Where(operation => operation.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(currency))
        {
            var normalizedCurrency = currency.Trim().ToUpperInvariant();
            query = query.Where(operation => operation.Money.Currency == normalizedCurrency);
        }
        if (from.HasValue) query = query.Where(operation => operation.CreatedAtUtc >= from.Value);
        if (to.HasValue) query = query.Where(operation => operation.CreatedAtUtc <= to.Value);
        var total = await query.CountAsync(cancellationToken);
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy.ToLowerInvariant(), descending) switch
        {
            ("amount", false) => query.OrderBy(operation => operation.Money.Amount).ThenBy(operation => operation.Id),
            ("amount", true) => query.OrderByDescending(operation => operation.Money.Amount).ThenBy(operation => operation.Id),
            ("status", false) => query.OrderBy(operation => operation.Status).ThenBy(operation => operation.Id),
            ("status", true) => query.OrderByDescending(operation => operation.Status).ThenBy(operation => operation.Id),
            ("createdat", false) => query.OrderBy(operation => operation.CreatedAtUtc).ThenBy(operation => operation.Id),
            _ => query.OrderByDescending(operation => operation.CreatedAtUtc).ThenBy(operation => operation.Id)
        };
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }
}
