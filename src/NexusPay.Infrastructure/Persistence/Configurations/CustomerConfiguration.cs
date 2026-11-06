using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusPay.Domain.Customers;

namespace NexusPay.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.CustomerCode)
            .HasColumnName("CustomerCodeNormalized").HasMaxLength(32).IsRequired();
        builder.Property(customer => customer.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(customer => customer.LastName).HasMaxLength(100).IsRequired();
        builder.Property(customer => customer.Email)
            .HasConversion(email => email.Value, value => EmailAddress.Create(value))
            .HasColumnName("Email").HasMaxLength(254).IsRequired();
        builder.Property(customer => customer.EmailNormalized).HasMaxLength(254).IsRequired();
        builder.Property(customer => customer.Phone).HasMaxLength(32);
        builder.Property(customer => customer.DocumentType).HasMaxLength(32);
        builder.Property(customer => customer.DocumentNumber).HasMaxLength(64);
        builder.Property(customer => customer.DocumentNumberNormalized).HasMaxLength(64);
        builder.Property(customer => customer.CreatedAtUtc).HasColumnType("datetimeoffset(3)");
        builder.Property(customer => customer.UpdatedAtUtc).HasColumnType("datetimeoffset(3)");
        builder.HasIndex(customer => customer.CustomerCode).IsUnique().HasDatabaseName("UX_Customers_CustomerCode");
        builder.HasIndex(customer => customer.EmailNormalized).IsUnique().HasDatabaseName("UX_Customers_EmailNormalized");
        builder.HasIndex(customer => new { customer.DocumentType, customer.DocumentNumberNormalized })
            .IsUnique().HasFilter("[DocumentNumberNormalized] IS NOT NULL")
            .HasDatabaseName("UX_Customers_Document");
    }
}
