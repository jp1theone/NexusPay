using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexusPay.Application.Common;
using NexusPay.Application.Customers;
using NexusPay.Application.Operations;
using NexusPay.Application.Auditing;
using NexusPay.Application.Reporting;
using NexusPay.Infrastructure.Persistence;
using NexusPay.Infrastructure.Simulations;

namespace NexusPay.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNexusPayInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("NexusPay")))
            throw new InvalidOperationException("Connection string 'NexusPay' is missing. Configure it with .NET User Secrets.");

        services.AddDbContext<NexusPayDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("NexusPay");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Connection string 'NexusPay' is missing. Configure it with .NET User Secrets.");
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
        });
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<NexusPayDbContext>());
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IFinancialOperationRepository, FinancialOperationRepository>();
        services.AddScoped<IPaymentProviderRepository, PaymentProviderRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IIntegrationLogRepository, IntegrationLogRepository>();
        services.AddScoped<IReadModelRepository, ReadModelRepository>();
        services.AddScoped<ReadModelUseCases>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IOperationCodeGenerator, OperationCodeGenerator>();
        var outcomeValue = configuration["PaymentSimulation:Outcome"] ?? nameof(PaymentSimulationOutcome.Approved);
        if (!Enum.TryParse<PaymentSimulationOutcome>(outcomeValue, ignoreCase: true, out var outcome))
            throw new InvalidOperationException("PaymentSimulation:Outcome must be Approved, Rejected, TechnicalError or Timeout.");
        services.AddSingleton<IPaymentSimulator>(new DeterministicPaymentSimulator(outcome,
            configuration["PaymentSimulation:ProviderCode"] ?? "NexusProviderAlpha"));
        return services;
    }
}
