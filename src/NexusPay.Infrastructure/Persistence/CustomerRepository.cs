using Microsoft.EntityFrameworkCore;
using NexusPay.Application.Customers;
using NexusPay.Domain.Customers;

namespace NexusPay.Infrastructure.Persistence;

public sealed class CustomerRepository(NexusPayDbContext context) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Customers.SingleOrDefaultAsync(customer => customer.Id == id, cancellationToken);

    public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        context.Customers.Add(customer);
        return Task.CompletedTask;
    }

    public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> SearchAsync(string? search, bool? isActive,
        int page, int pageSize, string sortBy, string sortDirection, CancellationToken cancellationToken = default)
    {
        var query = context.Customers.AsNoTracking().AsQueryable();
        if (isActive.HasValue) query = query.Where(customer => customer.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(customer => customer.FirstName.Contains(term) || customer.LastName.Contains(term) ||
                customer.EmailNormalized.Contains(term.ToUpperInvariant()) || customer.CustomerCode.Contains(term.ToUpperInvariant()));
        }
        var total = await query.CountAsync(cancellationToken);
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy.ToLowerInvariant(), descending) switch
        {
            ("customercode", false) => query.OrderBy(customer => customer.CustomerCode),
            ("customercode", true) => query.OrderByDescending(customer => customer.CustomerCode),
            ("lastname", false) => query.OrderBy(customer => customer.LastName).ThenBy(customer => customer.Id),
            ("lastname", true) => query.OrderByDescending(customer => customer.LastName).ThenBy(customer => customer.Id),
            ("createdat", false) => query.OrderBy(customer => customer.CreatedAtUtc).ThenBy(customer => customer.Id),
            _ => query.OrderByDescending(customer => customer.CreatedAtUtc).ThenBy(customer => customer.Id)
        };
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }
}
