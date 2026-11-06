using NexusPay.Application.Auditing;
using NexusPay.Domain.Auditing;

namespace NexusPay.Infrastructure.Persistence;

public sealed class AuditLogRepository(NexusPayDbContext context) : IAuditLogRepository
{
    public Task AddAsync(AuditLog entry, CancellationToken cancellationToken = default)
    {
        context.AuditLogs.Add(entry);
        return Task.CompletedTask;
    }
}
