using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusPay.Api.Middleware;
using NexusPay.Application.Auth;
using NexusPay.Application.Customers;

namespace NexusPay.Api.Controllers;

public sealed record SetCustomerStatusRequest(bool IsActive);

[ApiController]
[Route("api/customers")]
[Authorize(Roles = RoleNames.Administrator + "," + RoleNames.Operator + "," + RoleNames.Auditor)]
public sealed class CustomersController(
    CreateCustomerUseCase createCustomer,
    SearchCustomersUseCase searchCustomers,
    GetCustomerUseCase getCustomer,
    UpdateCustomerUseCase updateCustomer,
    ActivateCustomerUseCase activateCustomer,
    DeactivateCustomerUseCase deactivateCustomer,
    IValidator<UpdateCustomerRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageResult<CustomerDto>>> List([FromQuery] string? search = null,
        [FromQuery] bool? isActive = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "createdAt", [FromQuery] string sortDirection = "desc",
        CancellationToken cancellationToken = default) =>
        Ok(await searchCustomers.ExecuteAsync(search, isActive, page, pageSize, sortBy, sortDirection, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await getCustomer.ExecuteAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Roles = RoleNames.Administrator + "," + RoleNames.Operator)]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await createCustomer.ExecuteAsync(request, User.FindFirstValue(ClaimTypes.NameIdentifier), CorrelationId,
            cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = customer.Id }, customer);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleNames.Administrator + "," + RoleNames.Operator)]
    public async Task<ActionResult<CustomerDto>> Update(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await updateCustomer.ExecuteAsync(id, request, User.FindFirstValue(ClaimTypes.NameIdentifier),
            CorrelationId, cancellationToken));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> SetStatus(Guid id, SetCustomerStatusRequest request, CancellationToken cancellationToken)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (request.IsActive)
            await activateCustomer.ExecuteAsync(id, actor, CorrelationId, cancellationToken);
        else
            await deactivateCustomer.ExecuteAsync(id, actor, CorrelationId, cancellationToken);
        return NoContent();
    }

    private string? CorrelationId => HttpContext.Items[CorrelationIdMiddleware.ItemName]?.ToString();
}
