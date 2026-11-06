using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusPay.Domain.Operations;
using NexusPay.Infrastructure.Identity;

namespace NexusPay.Infrastructure.Persistence.Configurations;

public sealed class OperationStatusChangeConfiguration : IEntityTypeConfiguration<OperationStatusChange>
{
    public void Configure(EntityTypeBuilder<OperationStatusChange> builder)
    {
        builder.ToTable("OperationStatusHistory");
        builder.HasKey(change => change.Id);
        builder.Property(change => change.Id).ValueGeneratedOnAdd();
        builder.Property(change => change.FromStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(change => change.ToStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(change => change.ChangedAtUtc).HasColumnType("datetimeoffset(3)");
        builder.Property(change => change.Reason).HasMaxLength(200);
        builder.Property(change => change.ActorUserId).HasMaxLength(450);
        builder.Property(change => change.CorrelationId).HasMaxLength(128);
        builder.HasOne<NexusPayUser>().WithMany().HasForeignKey(change => change.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(change => new { change.OperationId, change.ChangedAtUtc });
    }
}
