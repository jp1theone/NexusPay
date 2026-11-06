using Microsoft.AspNetCore.Mvc;
using NexusPay.Infrastructure.Persistence;

namespace NexusPay.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController(NexusPayDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var databaseAvailable = await dbContext.Database.CanConnectAsync(cancellationToken);

        return databaseAvailable
            ? Ok(new { status = "healthy", database = "connected" })
            : StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { status = "unavailable", database = "disconnected" });
    }
}
