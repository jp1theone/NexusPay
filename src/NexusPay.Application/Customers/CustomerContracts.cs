using NexusPay.Domain.Customers;

namespace NexusPay.Application.Customers;

public sealed record CreateCustomerRequest(string CustomerCode, string FirstName, string LastName,
    string Email, string? Phone, string? DocumentType, string? DocumentNumber);

public sealed record CustomerDto(Guid Id, string CustomerCode, string FirstName, string LastName,
    string Email, string? Phone, string? DocumentType, string? DocumentNumber,
    bool IsActive, DateTimeOffset CreatedAtUtc);

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Customer> Items, int TotalCount)> SearchAsync(string? search, bool? isActive,
        int page, int pageSize, string sortBy, string sortDirection, CancellationToken cancellationToken = default);
}

public sealed record UpdateCustomerRequest(string FirstName, string LastName, string Email,
    string? Phone, string? DocumentType, string? DocumentNumber);

public sealed record PageResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
