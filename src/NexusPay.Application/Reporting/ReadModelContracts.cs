using NexusPay.Domain.Auditing;
using NexusPay.Domain.Integrations;
using NexusPay.Domain.Operations;
using NexusPay.Domain.Providers;
using NexusPay.Application.Operations;

namespace NexusPay.Application.Reporting;

public sealed record ProviderSummaryDto(Guid Id, string ProviderCode, string Name,
    PaymentProviderType Type, bool IsEnabled, bool IsAvailable);

public sealed record AuditLogDto(long Id, string? ActorUserId, string Action, string EntityType,
    string EntityId, DateTimeOffset OccurredAtUtc, string? CorrelationId, string Result, string? ChangeSummary);

public sealed record IntegrationLogDto(long Id, Guid ProviderId, Guid? PaymentAttemptId,
    string CorrelationId, IntegrationOutcome Outcome, string? ErrorCode, int DurationMilliseconds,
    DateTimeOffset OccurredAtUtc, string? TechnicalDetails);

public sealed record OperationReportRow(string OperationCode, string CustomerCode,
    OperationType Type, decimal Amount, string Currency, OperationStatus Status,
    string? ProviderCode, DateTimeOffset CreatedAtUtc, DateTimeOffset? ProcessedAtUtc);

public sealed record CustomerReportRow(string CustomerCode, string CustomerName, string Currency,
    int TotalOperations, int CompletedOperations, int FailedOperations, decimal CompletedVolume);

public sealed record ProviderReportRow(string ProviderCode, int TotalAttempts, int SuccessfulAttempts,
    int RejectedAttempts, int FailedAttempts, int TimedOutAttempts);

public sealed record CurrencyVolumeDto(string Currency, decimal Amount);

public sealed record DashboardSummaryDto(int TotalOperations, int CompletedOperations,
    int PendingOperations, int FailedOperations, decimal SuccessRatePercent,
    IReadOnlyList<CurrencyVolumeDto> SimulatedVolumeByCurrency,
    int AvailableProviders, int RecentIntegrationErrors,
    IReadOnlyList<FinancialOperationDto> RecentOperations);

public interface IReadModelRepository
{
    Task<(IReadOnlyList<PaymentProvider> Items, int TotalCount)> SearchProvidersAsync(
        int page, int pageSize, bool? enabled, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAuditLogsAsync(
        string? actorUserId, string? action, string? entityType, string? correlationId,
        DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<IntegrationLog> Items, int TotalCount)> SearchIntegrationLogsAsync(
        Guid? providerId, IntegrationOutcome? outcome, string? correlationId,
        DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize,
        CancellationToken cancellationToken = default);
    Task<DashboardSummaryDto> GetDashboardAsync(DateTimeOffset from, DateTimeOffset to,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<OperationReportRow> Items, int TotalCount)> GetOperationReportAsync(
        DateTimeOffset from, DateTimeOffset to, int page, int pageSize,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<CustomerReportRow> Items, int TotalCount)> GetCustomerReportAsync(
        DateTimeOffset from, DateTimeOffset to, int page, int pageSize,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ProviderReportRow> Items, int TotalCount)> GetProviderReportAsync(
        DateTimeOffset from, DateTimeOffset to, int page, int pageSize,
        CancellationToken cancellationToken = default);
}

public sealed record ReadPage<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
