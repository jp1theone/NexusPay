using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusPay.Api.Middleware;
using NexusPay.Application.Auth;
using NexusPay.Application.Providers;

namespace NexusPay.Api.Controllers;

[ApiController]
[Route("api/providers")]
[Authorize(Roles = RoleNames.Administrator)]
public sealed class ProvidersController(ProviderManagementUseCases management) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> Create(CreateProviderRequest request, CancellationToken cancellationToken)
    {
        var provider = await management.CreateAsync(request, User.FindFirstValue(ClaimTypes.NameIdentifier),
            CorrelationId, cancellationToken);
        return Created($"/api/providers/{provider.Id}", provider);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdateProviderRequest request, CancellationToken cancellationToken)
    {
        var provider = await management.UpdateAsync(id, request, User.FindFirstValue(ClaimTypes.NameIdentifier),
            CorrelationId, cancellationToken);
        return Ok(provider);
    }

    private string? CorrelationId => HttpContext.Items[CorrelationIdMiddleware.ItemName]?.ToString();
}
