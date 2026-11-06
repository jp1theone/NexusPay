using FluentValidation;
using NexusPay.Application.Auditing;
using NexusPay.Application.Common;
using NexusPay.Domain.Auditing;
using NexusPay.Domain.Customers;

namespace NexusPay.Application.Customers;

public sealed class CreateCustomerUseCase(
    IValidator<CreateCustomerRequest> validator,
    ICustomerRepository customers,
    IAuditLogRepository auditLogs,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<CustomerDto> ExecuteAsync(CreateCustomerRequest request, string? actorUserId = null,
        string? correlationId = null, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var customer = Customer.Create(request.CustomerCode, request.FirstName, request.LastName,
            request.Email, request.Phone, request.DocumentType, request.DocumentNumber, clock.UtcNow);
        await customers.AddAsync(customer, cancellationToken);
        await auditLogs.AddAsync(AuditLog.Create(actorUserId, "CustomerCreated", "Customer",
            customer.Id.ToString("D"), clock.UtcNow, correlationId ?? Guid.NewGuid().ToString("N"),
            "Succeeded", $"Created customer {customer.CustomerCode}."), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CustomerMapper.ToDto(customer);
    }
}

internal static class CustomerMapper
{
    public static CustomerDto ToDto(Customer customer) => new(customer.Id, customer.CustomerCode,
        customer.FirstName, customer.LastName, customer.Email.Value, customer.Phone,
        customer.DocumentType, customer.DocumentNumber, customer.IsActive, customer.CreatedAtUtc);
}
