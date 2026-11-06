using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusPay.Domain.Auditing;
using NexusPay.Infrastructure.Identity;

namespace NexusPay.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Id).ValueGeneratedOnAdd();
        builder.Property(log => log.ActorUserId).HasMaxLength(450);
        builder.Property(log => log.Action).HasMaxLength(80).IsRequired();
        builder.Property(log => log.EntityType).HasMaxLength(80).IsRequired();
        builder.Property(log => log.EntityId).HasMaxLength(128).IsRequired();
        builder.Property(log => log.OccurredAtUtc).HasColumnType("datetimeoffset(3)");
        builder.Property(log => log.CorrelationId).HasMaxLength(128);
        builder.Property(log => log.Result).HasMaxLength(32).IsRequired();
        builder.Property(log => log.ChangeSummary).HasMaxLength(1000);
        builder.HasIndex(log => log.OccurredAtUtc);
        builder.HasIndex(log => new { log.ActorUserId, log.OccurredAtUtc });
        builder.HasIndex(log => log.CorrelationId);
        builder.HasOne<NexusPayUser>().WithMany().HasForeignKey(log => log.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
