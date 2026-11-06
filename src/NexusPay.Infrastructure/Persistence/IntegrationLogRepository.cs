using NexusPay.Application.Auditing;
using NexusPay.Domain.Integrations;

namespace NexusPay.Infrastructure.Persistence;

public sealed class IntegrationLogRepository(NexusPayDbContext context) : IIntegrationLogRepository
{
    public Task AddAsync(IntegrationLog entry, CancellationToken cancellationToken = default)
    {
        context.IntegrationLogs.Add(entry);
        return Task.CompletedTask;
    }
}
