using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusPay.Application.Auth;
using NexusPay.Application.Reporting;
using NexusPay.Domain.Integrations;

namespace NexusPay.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class ReadModelsController(ReadModelUseCases readModels) : ControllerBase
{
    [Authorize]
    [HttpGet("providers")]
    public async Task<ActionResult<ReadPage<ProviderSummaryDto>>> Providers([FromQuery] bool? enabled = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(await readModels.ProvidersAsync(page, pageSize, enabled, cancellationToken));

    [Authorize(Roles = RoleNames.Administrator + "," + RoleNames.Auditor)]
    [HttpGet("audit-logs")]
    public async Task<ActionResult<ReadPage<AuditLogDto>>> AuditLogs([FromQuery] string? actorUserId = null,
        [FromQuery] string? action = null, [FromQuery] string? entityType = null,
        [FromQuery] string? correlationId = null, [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(await readModels.AuditLogsAsync(actorUserId, action, entityType, correlationId, from, to,
            page, pageSize, cancellationToken));

    [Authorize(Roles = RoleNames.Administrator + "," + RoleNames.Auditor)]
    [HttpGet("integration-logs")]
    public async Task<ActionResult<ReadPage<IntegrationLogDto>>> IntegrationLogs([FromQuery] Guid? providerId = null,
        [FromQuery] IntegrationOutcome? outcome = null, [FromQuery] string? correlationId = null,
        [FromQuery] DateTimeOffset? from = null, [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await readModels.IntegrationLogsAsync(providerId, outcome, correlationId, from, to,
            page, pageSize, cancellationToken));

    [Authorize(Roles = RoleNames.Administrator + "," + RoleNames.Operator + "," + RoleNames.Auditor)]
    [HttpGet("dashboard/summary")]
    public async Task<ActionResult<DashboardSummaryDto>> Dashboard([FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to, CancellationToken cancellationToken) =>
        Ok(await readModels.DashboardAsync(from, to, cancellationToken));

    [Authorize(Roles = RoleNames.Administrator + "," + RoleNames.Auditor)]
    [HttpGet("reports/operations")]
    public async Task<ActionResult<ReadPage<OperationReportRow>>> OperationReport([FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await readModels.OperationReportAsync(from, to, page, pageSize, cancellationToken));

    [Authorize(Roles = RoleNames.Administrator + "," + RoleNames.Auditor)]
    [HttpGet("reports/customers")]
    public async Task<ActionResult<ReadPage<CustomerReportRow>>> CustomerReport([FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await readModels.CustomerReportAsync(from, to, page, pageSize, cancellationToken));

    [Authorize(Roles = RoleNames.Administrator + "," + RoleNames.Auditor)]
    [HttpGet("reports/providers")]
    public async Task<ActionResult<ReadPage<ProviderReportRow>>> ProviderReport([FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await readModels.ProviderReportAsync(from, to, page, pageSize, cancellationToken));

    [Authorize(Roles = RoleNames.Administrator + "," + RoleNames.Auditor)]
    [HttpGet("reports/operations.csv")]
    public async Task<IActionResult> OperationReportCsv([FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
    {
        var rows = await CollectAsync((page, size) => readModels.OperationReportAsync(from, to, page, size, cancellationToken), cancellationToken);
        if (rows is null) return TooManyRows("operations");

        var csv = new System.Text.StringBuilder("OperationCode,CustomerCode,Type,Amount,Currency,Status,ProviderCode,CreatedAtUtc,ProcessedAtUtc\r\n");
        foreach (var row in rows)
        {
            csv.Append(Csv(row.OperationCode)).Append(',').Append(Csv(row.CustomerCode)).Append(',')
                .Append(row.Type).Append(',').Append(row.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                .Append(Csv(row.Currency)).Append(',').Append(row.Status).Append(',').Append(Csv(row.ProviderCode ?? string.Empty)).Append(',')
                .Append(row.CreatedAtUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                .Append(row.ProcessedAtUtc?.ToString("O", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty)
                .Append("\r\n");
        }
        return File(System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
            "text/csv; charset=utf-8", $"nexuspay-operations-{from:yyyyMMdd}-{to:yyyyMMdd}.csv");
    }

    [Authorize(Roles = RoleNames.Administrator + "," + RoleNames.Auditor)]
    [HttpGet("reports/customers.csv")]
    public async Task<IActionResult> CustomerReportCsv([FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
    {
        var rows = await CollectAsync((page, size) => readModels.CustomerReportAsync(from, to, page, size, cancellationToken), cancellationToken);
        if (rows is null) return TooManyRows("customers");
        var csv = new System.Text.StringBuilder("CustomerCode,CustomerName,Currency,TotalOperations,CompletedOperations,FailedOperations,CompletedVolume\r\n");
        foreach (var row in rows)
            csv.Append(Csv(row.CustomerCode)).Append(',').Append(Csv(row.CustomerName)).Append(',').Append(Csv(row.Currency)).Append(',')
                .Append(row.TotalOperations).Append(',').Append(row.CompletedOperations).Append(',')
                .Append(row.FailedOperations).Append(',').Append(row.CompletedVolume.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append("\r\n");
        return CsvFile(csv, $"nexuspay-customers-{from:yyyyMMdd}-{to:yyyyMMdd}.csv");
    }

    [Authorize(Roles = RoleNames.Administrator + "," + RoleNames.Auditor)]
    [HttpGet("reports/providers.csv")]
    public async Task<IActionResult> ProviderReportCsv([FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
    {
        var rows = await CollectAsync((page, size) => readModels.ProviderReportAsync(from, to, page, size, cancellationToken), cancellationToken);
        if (rows is null) return TooManyRows("providers");
        var csv = new System.Text.StringBuilder("ProviderCode,TotalAttempts,SuccessfulAttempts,RejectedAttempts,FailedAttempts,TimedOutAttempts\r\n");
        foreach (var row in rows)
            csv.Append(Csv(row.ProviderCode)).Append(',').Append(row.TotalAttempts).Append(',')
                .Append(row.SuccessfulAttempts).Append(',').Append(row.RejectedAttempts).Append(',')
                .Append(row.FailedAttempts).Append(',').Append(row.TimedOutAttempts).Append("\r\n");
        return CsvFile(csv, $"nexuspay-providers-{from:yyyyMMdd}-{to:yyyyMMdd}.csv");
    }

    private static string Csv(string value)
    {
        var firstValue = value.TrimStart();
        if (firstValue.Length > 0 && firstValue[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            value = $"'{value}";
        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }

    private IActionResult TooManyRows(string report) => StatusCode(StatusCodes.Status413PayloadTooLarge,
        new ProblemDetails { Status = 413, Title = $"Narrow the date range to export at most 10,000 {report} rows." });

    private static IActionResult CsvFile(System.Text.StringBuilder csv, string fileName) =>
        new FileContentResult(System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
            "text/csv; charset=utf-8") { FileDownloadName = fileName };

    private static async Task<List<T>?> CollectAsync<T>(
        Func<int, int, Task<ReadPage<T>>> getPage, CancellationToken cancellationToken)
    {
        const int pageSize = 100;
        var first = await getPage(1, pageSize);
        if (first.TotalCount > 10_000) return null;
        var rows = new List<T>(first.Items);
        for (var page = 2; rows.Count < first.TotalCount; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var next = await getPage(page, pageSize);
            rows.AddRange(next.Items);
        }
        return rows;
    }
}
