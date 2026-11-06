using Microsoft.EntityFrameworkCore;
using NexusPay.Application.Operations;
using NexusPay.Application.Reporting;
using NexusPay.Domain.Auditing;
using NexusPay.Domain.Integrations;
using NexusPay.Domain.Operations;
using NexusPay.Domain.Providers;

namespace NexusPay.Infrastructure.Persistence;

public sealed class ReadModelRepository(NexusPayDbContext context) : IReadModelRepository
{
    public async Task<(IReadOnlyList<PaymentProvider> Items, int TotalCount)> SearchProvidersAsync(
        int page, int pageSize, bool? enabled, CancellationToken cancellationToken = default)
    {
        var query = context.PaymentProviders.AsNoTracking().AsQueryable();
        if (enabled.HasValue) query = query.Where(provider => provider.IsEnabled == enabled.Value);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(provider => provider.ProviderCode)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAuditLogsAsync(
        string? actorUserId, string? action, string? entityType, string? correlationId,
        DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(actorUserId)) query = query.Where(entry => entry.ActorUserId == actorUserId.Trim());
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(entry => entry.Action == action.Trim());
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(entry => entry.EntityType == entityType.Trim());
        if (!string.IsNullOrWhiteSpace(correlationId)) query = query.Where(entry => entry.CorrelationId == correlationId.Trim());
        if (from.HasValue) query = query.Where(entry => entry.OccurredAtUtc >= from.Value);
        if (to.HasValue) query = query.Where(entry => entry.OccurredAtUtc <= to.Value);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(entry => entry.OccurredAtUtc).ThenByDescending(entry => entry.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(IReadOnlyList<IntegrationLog> Items, int TotalCount)> SearchIntegrationLogsAsync(
        Guid? providerId, IntegrationOutcome? outcome, string? correlationId,
        DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.IntegrationLogs.AsNoTracking().AsQueryable();
        if (providerId.HasValue) query = query.Where(entry => entry.PaymentProviderId == providerId.Value);
        if (outcome.HasValue) query = query.Where(entry => entry.Outcome == outcome.Value);
        if (!string.IsNullOrWhiteSpace(correlationId)) query = query.Where(entry => entry.CorrelationId == correlationId.Trim());
        if (from.HasValue) query = query.Where(entry => entry.OccurredAtUtc >= from.Value);
        if (to.HasValue) query = query.Where(entry => entry.OccurredAtUtc <= to.Value);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(entry => entry.OccurredAtUtc).ThenByDescending(entry => entry.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<DashboardSummaryDto> GetDashboardAsync(DateTimeOffset from, DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var operations = context.FinancialOperations.AsNoTracking()
            .Where(operation => operation.CreatedAtUtc >= from && operation.CreatedAtUtc <= to);
        var total = await operations.CountAsync(cancellationToken);
        var completed = await operations.CountAsync(operation => operation.Status == OperationStatus.Completed, cancellationToken);
        var pending = await operations.CountAsync(operation => operation.Status == OperationStatus.Pending ||
            operation.Status == OperationStatus.Processing, cancellationToken);
        var failed = await operations.CountAsync(operation => operation.Status == OperationStatus.Failed, cancellationToken);
        var volumeRows = await operations.Where(operation => operation.Status == OperationStatus.Completed)
            .GroupBy(operation => operation.Money.Currency)
            .Select(group => new { Currency = group.Key, Amount = group.Sum(operation => operation.Money.Amount) })
            .ToListAsync(cancellationToken);
        var volume = volumeRows.OrderBy(entry => entry.Currency)
            .Select(entry => new CurrencyVolumeDto(entry.Currency, entry.Amount)).ToArray();
        var decisionCount = completed + failed;
        var successRate = decisionCount == 0 ? 0m : decimal.Round((decimal)completed * 100m / decisionCount, 2);
        var availableProviders = await context.PaymentProviders.AsNoTracking()
            .CountAsync(provider => provider.IsEnabled && provider.IsAvailable, cancellationToken);
        var errors = await context.IntegrationLogs.AsNoTracking().CountAsync(entry =>
            (entry.Outcome == IntegrationOutcome.Failed || entry.Outcome == IntegrationOutcome.TimedOut) &&
            entry.OccurredAtUtc >= from && entry.OccurredAtUtc <= to, cancellationToken);
        var recent = await context.FinancialOperations.AsNoTracking().Where(operation =>
                operation.CreatedAtUtc >= from && operation.CreatedAtUtc <= to)
            .OrderByDescending(operation => operation.CreatedAtUtc).ThenByDescending(operation => operation.Id)
            .Take(10).Select(operation => new FinancialOperationDto(operation.Id, operation.OperationCode,
                operation.CustomerId, operation.Type, operation.Money.Amount, operation.Money.Currency,
                operation.Status, operation.Description, operation.ProcessingAttempts,
                operation.LastProviderCode, operation.LastProviderReference, operation.LastFailureCode,
                operation.CreatedAtUtc, operation.ProcessedAtUtc)).ToListAsync(cancellationToken);
        return new DashboardSummaryDto(total, completed, pending, failed, successRate, volume,
            availableProviders, errors, recent);
    }

    public async Task<(IReadOnlyList<OperationReportRow> Items, int TotalCount)> GetOperationReportAsync(
        DateTimeOffset fromUtc, DateTimeOffset toUtc, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = from operation in context.FinancialOperations.AsNoTracking()
                    join customer in context.Customers.AsNoTracking() on operation.CustomerId equals customer.Id
                    where operation.CreatedAtUtc >= fromUtc && operation.CreatedAtUtc <= toUtc
                    select new { Operation = operation, Customer = customer };
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(row => row.Operation.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(row => new OperationReportRow(row.Operation.OperationCode, row.Customer.CustomerCode,
                row.Operation.Type, row.Operation.Money.Amount, row.Operation.Money.Currency,
                row.Operation.Status, row.Operation.LastProviderCode, row.Operation.CreatedAtUtc,
                row.Operation.ProcessedAtUtc)).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(IReadOnlyList<CustomerReportRow> Items, int TotalCount)> GetCustomerReportAsync(
        DateTimeOffset fromUtc, DateTimeOffset toUtc, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var operations = from operation in context.FinancialOperations.AsNoTracking()
                         join customer in context.Customers.AsNoTracking() on operation.CustomerId equals customer.Id
                         where operation.CreatedAtUtc >= fromUtc && operation.CreatedAtUtc <= toUtc
                         select new
                         {
                             customer.Id, customer.CustomerCode, customer.FirstName, customer.LastName,
                             operation.Money.Currency,
                             operation.Status, Amount = operation.Money.Amount
                         };
        var grouped = operations.GroupBy(row => new
            { row.Id, row.CustomerCode, row.FirstName, row.LastName, row.Currency })
            .Select(rows => new
            {
                rows.Key.CustomerCode,
                CustomerName = rows.Key.FirstName + " " + rows.Key.LastName,
                rows.Key.Currency,
                Total = rows.Count(),
                Completed = rows.Sum(row => row.Status == OperationStatus.Completed ? 1 : 0),
                Failed = rows.Sum(row => row.Status == OperationStatus.Failed ? 1 : 0),
                Volume = rows.Sum(row => row.Status == OperationStatus.Completed ? row.Amount : 0m)
            });
        var total = await grouped.CountAsync(cancellationToken);
        var results = await grouped.OrderBy(row => row.CustomerCode).Skip((page - 1) * pageSize)
            .Take(pageSize).ToListAsync(cancellationToken);
        var items = results.Select(row => new CustomerReportRow(row.CustomerCode, row.CustomerName,
            row.Currency, row.Total, row.Completed, row.Failed, row.Volume)).ToArray();
        return (items, total);
    }

    public async Task<(IReadOnlyList<ProviderReportRow> Items, int TotalCount)> GetProviderReportAsync(
        DateTimeOffset fromUtc, DateTimeOffset toUtc, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var attempts = from attempt in context.PaymentAttempts.AsNoTracking()
                       join provider in context.PaymentProviders.AsNoTracking()
                           on attempt.PaymentProviderId equals provider.Id
                       where attempt.StartedAtUtc >= fromUtc && attempt.StartedAtUtc <= toUtc
                       select new { provider.ProviderCode, attempt.Status };
        var grouped = attempts.GroupBy(attempt => attempt.ProviderCode).Select(rows => new
        {
            ProviderCode = rows.Key,
            Total = rows.Count(),
            Successful = rows.Sum(attempt => attempt.Status == PaymentAttemptStatus.Succeeded ? 1 : 0),
            Rejected = rows.Sum(attempt => attempt.Status == PaymentAttemptStatus.Rejected ? 1 : 0),
            Failed = rows.Sum(attempt => attempt.Status == PaymentAttemptStatus.Failed ? 1 : 0),
            TimedOut = rows.Sum(attempt => attempt.Status == PaymentAttemptStatus.TimedOut ? 1 : 0)
        });
        var total = await grouped.CountAsync(cancellationToken);
        var results = await grouped.OrderBy(row => row.ProviderCode).Skip((page - 1) * pageSize)
            .Take(pageSize).ToListAsync(cancellationToken);
        var items = results.Select(row => new ProviderReportRow(row.ProviderCode, row.Total,
            row.Successful, row.Rejected, row.Failed, row.TimedOut)).ToArray();
        return (items, total);
    }
}
