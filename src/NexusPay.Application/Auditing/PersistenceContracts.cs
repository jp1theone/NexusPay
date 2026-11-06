using NexusPay.Domain.Auditing;
using NexusPay.Domain.Integrations;

namespace NexusPay.Application.Auditing;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog entry, CancellationToken cancellationToken = default);
}

public interface IIntegrationLogRepository
{
    Task AddAsync(IntegrationLog entry, CancellationToken cancellationToken = default);
}
