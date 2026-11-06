using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusPay.Api.Middleware;
using NexusPay.Application.Auth;
using NexusPay.Application.Customers;
using NexusPay.Application.Operations;
using NexusPay.Domain.Operations;

namespace NexusPay.Api.Controllers;

public sealed record CreateOperationBody(Guid CustomerId, OperationType Type, decimal Amount,
    string Currency, string? Description);

[ApiController]
[Route("api/operations")]
[Authorize(Roles = RoleNames.Administrator + "," + RoleNames.Operator + "," + RoleNames.Auditor)]
public sealed class OperationsController(
    CreateFinancialOperationUseCase createOperation,
    SearchFinancialOperationsUseCase searchOperations,
    GetFinancialOperationUseCase getOperation,
    CancelFinancialOperationUseCase cancelOperation,
    ProcessFinancialOperationUseCase processOperation) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageResult<FinancialOperationDto>>> List([FromQuery] Guid? customerId = null,
        [FromQuery] OperationStatus? status = null, [FromQuery] string? currency = null,
        [FromQuery] DateTimeOffset? from = null, [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "createdAt", [FromQuery] string sortDirection = "desc",
        CancellationToken cancellationToken = default) =>
        Ok(await searchOperations.ExecuteAsync(customerId, status, currency, from, to, page, pageSize,
            sortBy, sortDirection, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FinancialOperationDetailsDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await getOperation.ExecuteAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Roles = RoleNames.Administrator + "," + RoleNames.Operator)]
    public async Task<ActionResult<FinancialOperationDto>> Create(CreateOperationBody body,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await createOperation.ExecuteWithResultAsync(new CreateFinancialOperationRequest(body.CustomerId,
                body.Type, body.Amount, body.Currency, body.Description, idempotencyKey ?? string.Empty),
            User.FindFirstValue(ClaimTypes.NameIdentifier), CorrelationId, cancellationToken);
        if (result.IsReplay) return Ok(result.Operation);
        return CreatedAtAction(nameof(Get), new { id = result.Operation.Id }, result.Operation);
    }

    [HttpPost("{id:guid}/process")]
    [Authorize(Roles = RoleNames.Administrator + "," + RoleNames.Operator)]
    public async Task<ActionResult<FinancialOperationDto>> Process(Guid id,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        Ok(await processOperation.ExecuteAsync(id, new ProcessFinancialOperationRequest(idempotencyKey ?? string.Empty),
            User.FindFirstValue(ClaimTypes.NameIdentifier), CorrelationId, cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = RoleNames.Administrator + "," + RoleNames.Operator)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await cancelOperation.ExecuteAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier), CorrelationId, cancellationToken);
        return NoContent();
    }

    private string? CorrelationId => HttpContext.Items[CorrelationIdMiddleware.ItemName]?.ToString();
}
