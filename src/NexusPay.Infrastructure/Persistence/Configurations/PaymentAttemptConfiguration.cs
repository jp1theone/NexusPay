using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusPay.Domain.Operations;

namespace NexusPay.Infrastructure.Persistence.Configurations;

public sealed class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
{
    public void Configure(EntityTypeBuilder<PaymentAttempt> builder)
    {
        builder.ToTable("PaymentAttempts");
        builder.HasKey(attempt => attempt.Id);
        builder.Property(attempt => attempt.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(attempt => attempt.ProviderCode).HasMaxLength(64).IsRequired();
        builder.Property(attempt => attempt.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(attempt => attempt.ProviderReference).HasMaxLength(128);
        builder.Property(attempt => attempt.FailureCode).HasMaxLength(80);
        builder.Property(attempt => attempt.StartedAtUtc).HasColumnType("datetimeoffset(3)");
        builder.Property(attempt => attempt.CompletedAtUtc).HasColumnType("datetimeoffset(3)");
        builder.HasOne<NexusPay.Domain.Providers.PaymentProvider>().WithMany()
            .HasForeignKey(attempt => attempt.PaymentProviderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(attempt => new { attempt.OperationId, attempt.AttemptNumber }).IsUnique();
        builder.HasIndex(attempt => attempt.IdempotencyKey).IsUnique();
    }
}
