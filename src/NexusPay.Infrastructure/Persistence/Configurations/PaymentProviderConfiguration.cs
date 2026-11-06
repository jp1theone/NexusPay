using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusPay.Domain.Providers;
using NexusPay.Infrastructure.Simulations;

namespace NexusPay.Infrastructure.Persistence.Configurations;

public sealed class PaymentProviderConfiguration : IEntityTypeConfiguration<PaymentProvider>
{
    public void Configure(EntityTypeBuilder<PaymentProvider> builder)
    {
        builder.ToTable("PaymentProviders");
        builder.HasKey(provider => provider.Id);
        builder.Property(provider => provider.ProviderCode).HasMaxLength(64).IsRequired();
        builder.Property(provider => provider.Name).HasMaxLength(120).IsRequired();
        builder.Property(provider => provider.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(provider => provider.ConfigurationJson).HasMaxLength(2000);
        builder.Property(provider => provider.CreatedAtUtc).HasColumnType("datetimeoffset(3)");
        builder.Property(provider => provider.UpdatedAtUtc).HasColumnType("datetimeoffset(3)");
        builder.HasIndex(provider => provider.ProviderCode).IsUnique();
        builder.HasData(new
        {
            Id = DeterministicPaymentSimulator.DefaultProviderId,
            ProviderCode = "NEXUSPROVIDERALPHA",
            Name = "Nexus Provider Alpha",
            Type = PaymentProviderType.Simulator,
            IsEnabled = true,
            IsAvailable = true,
            ConfigurationJson = (string?)null,
            CreatedAtUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            UpdatedAtUtc = (DateTimeOffset?)null
        });
    }
}
