using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using NexusPay.Application.Common;
using NexusPay.Infrastructure.Identity;

namespace NexusPay.Infrastructure.Persistence;

public sealed class NexusPayDbContext(DbContextOptions<NexusPayDbContext> options)
    : IdentityDbContext<NexusPayUser>(options), IUnitOfWork
{
    public DbSet<NexusPay.Domain.Customers.Customer> Customers => Set<NexusPay.Domain.Customers.Customer>();
    public DbSet<NexusPay.Domain.Operations.FinancialOperation> FinancialOperations => Set<NexusPay.Domain.Operations.FinancialOperation>();
    public DbSet<NexusPay.Domain.Operations.OperationStatusChange> OperationStatusHistory => Set<NexusPay.Domain.Operations.OperationStatusChange>();
    public DbSet<NexusPay.Domain.Operations.PaymentAttempt> PaymentAttempts => Set<NexusPay.Domain.Operations.PaymentAttempt>();
    public DbSet<NexusPay.Domain.Providers.PaymentProvider> PaymentProviders => Set<NexusPay.Domain.Providers.PaymentProvider>();
    public DbSet<NexusPay.Domain.Integrations.IntegrationLog> IntegrationLogs => Set<NexusPay.Domain.Integrations.IntegrationLog>();
    public DbSet<NexusPay.Domain.Auditing.AuditLog> AuditLogs => Set<NexusPay.Domain.Auditing.AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NexusPayDbContext).Assembly);
        modelBuilder.Entity<NexusPayUser>(entity =>
        {
            entity.ToTable("AspNetUsers");
            entity.Property(user => user.CreatedAtUtc).HasColumnType("datetimeoffset(3)");
            entity.HasIndex(user => user.NormalizedEmail).IsUnique().HasFilter("[NormalizedEmail] IS NOT NULL")
                .HasDatabaseName("EmailIndex");
        });
        modelBuilder.Entity<IdentityRole>().ToTable("AspNetRoles");
        modelBuilder.Entity<IdentityUserRole<string>>().ToTable("AspNetUserRoles");
        modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("AspNetUserClaims");
        modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("AspNetUserLogins");
        modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("AspNetRoleClaims");
        modelBuilder.Entity<IdentityUserToken<string>>().ToTable("AspNetUserTokens");
    }
}
