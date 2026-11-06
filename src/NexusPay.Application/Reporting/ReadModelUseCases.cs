using NexusPay.Domain.Common;
using NexusPay.Domain.Integrations;
using NexusPay.Application.Operations;

namespace NexusPay.Application.Reporting;

public sealed class ReadModelUseCases(IReadModelRepository repository)
{
    public async Task<ReadPage<ProviderSummaryDto>> ProvidersAsync(int page, int pageSize, bool? enabled,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(page, pageSize);
        var result = await repository.SearchProvidersAsync(page, pageSize, enabled, cancellationToken);
        return new ReadPage<ProviderSummaryDto>(result.Items.Select(provider => new ProviderSummaryDto(
            provider.Id, provider.ProviderCode, provider.Name, provider.Type,
            provider.IsEnabled, provider.IsAvailable)).ToArray(), result.TotalCount, page, pageSize);
    }

    public async Task<ReadPage<AuditLogDto>> AuditLogsAsync(string? actorUserId, string? action,
        string? entityType, string? correlationId, DateTimeOffset? from, DateTimeOffset? to,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ValidatePage(page, pageSize);
        ValidateDateRange(from, to, 366);
        var result = await repository.SearchAuditLogsAsync(actorUserId, action, entityType,
            correlationId, from?.ToUniversalTime(), to?.ToUniversalTime(), page, pageSize, cancellationToken);
        return new ReadPage<AuditLogDto>(result.Items.Select(entry => new AuditLogDto(entry.Id,
            entry.ActorUserId, entry.Action, entry.EntityType, entry.EntityId, entry.OccurredAtUtc,
            entry.CorrelationId, entry.Result, entry.ChangeSummary)).ToArray(), result.TotalCount, page, pageSize);
    }

    public async Task<ReadPage<IntegrationLogDto>> IntegrationLogsAsync(Guid? providerId,
        IntegrationOutcome? outcome, string? correlationId, DateTimeOffset? from, DateTimeOffset? to,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ValidatePage(page, pageSize);
        ValidateDateRange(from, to, 366);
        var result = await repository.SearchIntegrationLogsAsync(providerId, outcome, correlationId,
            from?.ToUniversalTime(), to?.ToUniversalTime(), page, pageSize, cancellationToken);
        return new ReadPage<IntegrationLogDto>(result.Items.Select(entry => new IntegrationLogDto(
            entry.Id, entry.PaymentProviderId, entry.PaymentAttemptId, entry.CorrelationId, entry.Outcome,
            entry.ErrorCode, entry.DurationMilliseconds, entry.OccurredAtUtc, entry.TechnicalDetails)).ToArray(),
            result.TotalCount, page, pageSize);
    }

    public Task<DashboardSummaryDto> DashboardAsync(DateTimeOffset from, DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        ValidateDateRange(from, to, 366, required: true);
        return repository.GetDashboardAsync(from.ToUniversalTime(), to.ToUniversalTime(), cancellationToken);
    }

    public async Task<ReadPage<OperationReportRow>> OperationReportAsync(DateTimeOffset from,
        DateTimeOffset to, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ValidatePage(page, pageSize);
        if (pageSize > 100) throw new DomainRuleException("Page size cannot exceed 100.");
        ValidateDateRange(from, to, 366, required: true);
        var result = await repository.GetOperationReportAsync(from.ToUniversalTime(), to.ToUniversalTime(),
            page, pageSize, cancellationToken);
        return new ReadPage<OperationReportRow>(result.Items, result.TotalCount, page, pageSize);
    }

    public async Task<ReadPage<CustomerReportRow>> CustomerReportAsync(DateTimeOffset from,
        DateTimeOffset to, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ValidatePage(page, pageSize);
        ValidateDateRange(from, to, 366, required: true);
        var result = await repository.GetCustomerReportAsync(from.ToUniversalTime(), to.ToUniversalTime(),
            page, pageSize, cancellationToken);
        return new ReadPage<CustomerReportRow>(result.Items, result.TotalCount, page, pageSize);
    }

    public async Task<ReadPage<ProviderReportRow>> ProviderReportAsync(DateTimeOffset from,
        DateTimeOffset to, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ValidatePage(page, pageSize);
        ValidateDateRange(from, to, 366, required: true);
        var result = await repository.GetProviderReportAsync(from.ToUniversalTime(), to.ToUniversalTime(),
            page, pageSize, cancellationToken);
        return new ReadPage<ProviderReportRow>(result.Items, result.TotalCount, page, pageSize);
    }

    private static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            throw new DomainRuleException("Page must be positive and page size must be between 1 and 100.");
    }

    private static void ValidateDateRange(DateTimeOffset? from, DateTimeOffset? to,
        int maxDays, bool required = false)
    {
        if (required && (!from.HasValue || !to.HasValue))
            throw new DomainRuleException("Both from and to dates are required.");
        if (from.HasValue && to.HasValue && from.Value > to.Value)
            throw new DomainRuleException("The from date must not be later than the to date.");
        if (from.HasValue && to.HasValue && to.Value - from.Value > TimeSpan.FromDays(maxDays))
            throw new DomainRuleException($"The requested date range cannot exceed {maxDays} days.");
    }
}
