using Microsoft.EntityFrameworkCore;
using NexusPay.Application.Operations;
using NexusPay.Domain.Providers;

namespace NexusPay.Infrastructure.Persistence;

public sealed class PaymentProviderRepository(NexusPayDbContext context) : IPaymentProviderRepository
{
    public Task<PaymentProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.PaymentProviders.SingleOrDefaultAsync(provider => provider.Id == id, cancellationToken);

    public Task<PaymentProvider?> GetByCodeAsync(string providerCode, CancellationToken cancellationToken = default)
    {
        var normalized = providerCode.Trim().ToUpperInvariant();
        return context.PaymentProviders.SingleOrDefaultAsync(provider => provider.ProviderCode == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentProvider>> GetEnabledAsync(CancellationToken cancellationToken = default) =>
        await context.PaymentProviders.Where(provider => provider.IsEnabled && provider.IsAvailable)
            .OrderBy(provider => provider.ProviderCode).ToListAsync(cancellationToken);

    public async Task AddAsync(PaymentProvider provider, CancellationToken cancellationToken = default) =>
        await context.PaymentProviders.AddAsync(provider, cancellationToken);
}
