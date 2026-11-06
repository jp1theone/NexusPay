using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusPay.Domain.Operations;

namespace NexusPay.Infrastructure.Persistence.Configurations;

public sealed class FinancialOperationConfiguration : IEntityTypeConfiguration<FinancialOperation>
{
    public void Configure(EntityTypeBuilder<FinancialOperation> builder)
    {
        builder.ToTable("FinancialOperations", table =>
        {
            table.HasCheckConstraint("CK_FinancialOperations_Amount", "[Amount] > 0");
            table.HasCheckConstraint("CK_FinancialOperations_Currency", "[Currency] IN ('PEN', 'USD', 'EUR')");
        });
        builder.HasKey(operation => operation.Id);
        builder.Property(operation => operation.OperationCode).HasColumnName("OperationCodeNormalized").HasMaxLength(64).IsRequired();
        builder.Property(operation => operation.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(operation => operation.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(operation => operation.Description).HasMaxLength(500);
        builder.Property(operation => operation.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(operation => operation.RequestHash).HasMaxLength(64).IsRequired();
        builder.Property(operation => operation.LastProcessIdempotencyKey).HasMaxLength(128);
        builder.Property(operation => operation.LastProviderCode).HasMaxLength(64);
        builder.Property(operation => operation.LastProviderReference).HasMaxLength(128);
        builder.Property(operation => operation.LastFailureCode).HasMaxLength(80);
        builder.Property(operation => operation.CreatedAtUtc).HasColumnType("datetimeoffset(3)");
        builder.Property(operation => operation.UpdatedAtUtc).HasColumnType("datetimeoffset(3)");
        builder.Property(operation => operation.ProcessedAtUtc).HasColumnType("datetimeoffset(3)");
        builder.Property(operation => operation.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.OwnsOne(operation => operation.Money, money =>
        {
            money.Property(value => value.Amount).HasColumnName("Amount").HasPrecision(18, 2);
            money.Property(value => value.Currency).HasColumnName("Currency").HasColumnType("char(3)").IsRequired();
        });
        builder.HasOne<NexusPay.Domain.Customers.Customer>().WithMany()
            .HasForeignKey(operation => operation.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(operation => operation.OperationCode).IsUnique().HasDatabaseName("UX_FinancialOperations_Code");
        builder.HasIndex(operation => operation.IdempotencyKey).IsUnique().HasDatabaseName("UX_FinancialOperations_IdempotencyKey");
        builder.HasIndex(operation => new { operation.CreatedAtUtc, operation.Id });
        builder.HasIndex(operation => new { operation.Status, operation.CreatedAtUtc });
        builder.HasIndex(operation => new { operation.CustomerId, operation.CreatedAtUtc });

        builder.HasMany(operation => operation.StatusHistory).WithOne()
            .HasForeignKey(change => change.OperationId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(operation => operation.StatusHistory).HasField("_statusHistory")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(operation => operation.PaymentAttempts).WithOne()
            .HasForeignKey(attempt => attempt.OperationId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(operation => operation.PaymentAttempts).HasField("_paymentAttempts")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
