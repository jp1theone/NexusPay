using NexusPay.Application.Auditing;
using NexusPay.Application.Common;
using NexusPay.Application.Operations;
using NexusPay.Application.Reporting;
using NexusPay.Domain.Auditing;
using NexusPay.Domain.Common;
using NexusPay.Domain.Providers;

namespace NexusPay.Application.Providers;

public sealed record CreateProviderRequest(string ProviderCode, string Name);
public sealed record UpdateProviderRequest(string Name, bool IsEnabled, bool IsAvailable);

public sealed class ProviderManagementUseCases(IPaymentProviderRepository providers,
    IAuditLogRepository auditLogs, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task<ProviderSummaryDto> CreateAsync(CreateProviderRequest request, string? actorUserId,
        string? correlationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderCode) || request.ProviderCode.Trim().Length > 64)
            throw new DomainRuleException("Provider code is required and must be at most 64 characters.");
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 120)
            throw new DomainRuleException("Provider name is required and must be at most 120 characters.");
        var code = request.ProviderCode.Trim().ToUpperInvariant();
        if (await providers.GetByCodeAsync(code, cancellationToken) is not null)
            throw new ConflictException("A provider with this code already exists.");

        var provider = PaymentProvider.Create(Guid.NewGuid(), code, request.Name, clock.UtcNow);
        await providers.AddAsync(provider, cancellationToken);
        await auditLogs.AddAsync(AuditLog.Create(actorUserId, "provider.created", "PaymentProvider",
            provider.Id.ToString("D"), clock.UtcNow, correlationId, "Success", $"Provider {code} created."), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(provider);
    }

    public async Task<ProviderSummaryDto> UpdateAsync(Guid id, UpdateProviderRequest request, string? actorUserId,
        string? correlationId, CancellationToken cancellationToken = default)
    {
        var provider = await providers.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Provider was not found.");
        provider.Configure(request.Name, request.IsEnabled, request.IsAvailable, clock.UtcNow);
        await auditLogs.AddAsync(AuditLog.Create(actorUserId, "provider.updated", "PaymentProvider",
            provider.Id.ToString("D"), clock.UtcNow, correlationId, "Success",
            $"Provider configuration updated. Enabled={provider.IsEnabled}; available={provider.IsAvailable}."), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(provider);
    }

    private static ProviderSummaryDto ToDto(PaymentProvider provider) => new(provider.Id,
        provider.ProviderCode, provider.Name, provider.Type, provider.IsEnabled, provider.IsAvailable);
}
