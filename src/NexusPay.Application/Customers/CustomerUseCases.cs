using FluentValidation;
using NexusPay.Application.Common;
using NexusPay.Application.Auditing;
using NexusPay.Domain.Auditing;
using NexusPay.Domain.Common;

namespace NexusPay.Application.Customers;

public sealed class SearchCustomersUseCase(ICustomerRepository customers)
{
    public async Task<PageResult<CustomerDto>> ExecuteAsync(string? search, bool? isActive,
        int page, int pageSize, string sortBy, string sortDirection, CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100) throw new DomainRuleException("Page must be positive and page size must be between 1 and 100.");
        var allowedSorts = new[] { "createdAt", "customerCode", "lastName" };
        if (!allowedSorts.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            throw new DomainRuleException("The requested customer sort field is not supported.");
        if (!new[] { "asc", "desc" }.Contains(sortDirection, StringComparer.OrdinalIgnoreCase))
            throw new DomainRuleException("Sort direction must be asc or desc.");
        var result = await customers.SearchAsync(search, isActive, page, pageSize, sortBy, sortDirection, cancellationToken);
        return new PageResult<CustomerDto>(result.Items.Select(CustomerMapper.ToDto).ToArray(), result.TotalCount, page, pageSize);
    }
}

public sealed class GetCustomerUseCase(ICustomerRepository customers)
{
    public async Task<CustomerDto> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Customer was not found.");
        return CustomerMapper.ToDto(customer);
    }
}

public sealed class UpdateCustomerUseCase(
    ICustomerRepository customers, IAuditLogRepository auditLogs, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task<CustomerDto> ExecuteAsync(Guid id, UpdateCustomerRequest request, string? actorUserId = null,
        string? correlationId = null, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Customer was not found.");
        customer.Update(request.FirstName, request.LastName, request.Email, request.Phone,
            request.DocumentType, request.DocumentNumber, clock.UtcNow);
        await auditLogs.AddAsync(AuditLog.Create(actorUserId, "CustomerUpdated", "Customer", id.ToString("D"),
            clock.UtcNow, correlationId ?? Guid.NewGuid().ToString("N"), "Succeeded", "Customer profile updated."), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CustomerMapper.ToDto(customer);
    }
}

public sealed class DeactivateCustomerUseCase(ICustomerRepository customers, IAuditLogRepository auditLogs, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task ExecuteAsync(Guid id, string? actorUserId = null, string? correlationId = null, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Customer was not found.");
        customer.Deactivate(clock.UtcNow);
        await auditLogs.AddAsync(AuditLog.Create(actorUserId, "CustomerDeactivated", "Customer", id.ToString("D"),
            clock.UtcNow, correlationId ?? Guid.NewGuid().ToString("N"), "Succeeded"), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ActivateCustomerUseCase(ICustomerRepository customers, IAuditLogRepository auditLogs, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task ExecuteAsync(Guid id, string? actorUserId = null, string? correlationId = null, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Customer was not found.");
        customer.Activate(clock.UtcNow);
        await auditLogs.AddAsync(AuditLog.Create(actorUserId, "CustomerActivated", "Customer", id.ToString("D"),
            clock.UtcNow, correlationId ?? Guid.NewGuid().ToString("N"), "Succeeded"), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Phone).MaximumLength(32);
        RuleFor(x => x.DocumentType).MaximumLength(32);
        RuleFor(x => x.DocumentNumber).MaximumLength(64);
        RuleFor(x => x).Must(x => string.IsNullOrWhiteSpace(x.DocumentType) == string.IsNullOrWhiteSpace(x.DocumentNumber))
            .WithMessage("Document type and number must either both be provided or both be omitted.");
    }
}
