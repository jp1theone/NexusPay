using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusPay.Domain.Integrations;

namespace NexusPay.Infrastructure.Persistence.Configurations;

public sealed class IntegrationLogConfiguration : IEntityTypeConfiguration<IntegrationLog>
{
    public void Configure(EntityTypeBuilder<IntegrationLog> builder)
    {
        builder.ToTable("IntegrationLogs");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Id).ValueGeneratedOnAdd();
        builder.Property(log => log.CorrelationId).HasMaxLength(128).IsRequired();
        builder.Property(log => log.Outcome).HasConversion<string>().HasMaxLength(32);
        builder.Property(log => log.ErrorCode).HasMaxLength(80);
        builder.Property(log => log.TechnicalDetails).HasMaxLength(2000);
        builder.Property(log => log.OccurredAtUtc).HasColumnType("datetimeoffset(3)");
        builder.HasOne<NexusPay.Domain.Providers.PaymentProvider>().WithMany()
            .HasForeignKey(log => log.PaymentProviderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<NexusPay.Domain.Operations.PaymentAttempt>().WithMany()
            .HasForeignKey(log => log.PaymentAttemptId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(log => new { log.PaymentProviderId, log.OccurredAtUtc });
        builder.HasIndex(log => log.CorrelationId);
    }
}
