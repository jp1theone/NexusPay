using NexusPay.Domain.Common;

namespace NexusPay.Domain.Customers;

public sealed class Customer
{
    private Customer() { }

    private Customer(Guid id, CustomerCode code, string firstName, string lastName,
        EmailAddress email, string? phone, string? documentType, string? documentNumber, DateTimeOffset createdAtUtc)
    {
        Id = id;
        CustomerCode = code.Value;
        FirstName = RequiredName(firstName, nameof(firstName));
        LastName = RequiredName(lastName, nameof(lastName));
        Email = email;
        EmailNormalized = email.NormalizedValue;
        Phone = NormalizeOptional(phone, 32, nameof(phone));
        DocumentType = NormalizeOptional(documentType, 32, nameof(documentType));
        DocumentNumber = NormalizeOptional(documentNumber, 64, nameof(documentNumber));
        DocumentNumberNormalized = DocumentNumber?.ToUpperInvariant();
        if ((DocumentType is null) != (DocumentNumber is null))
        {
            throw new DomainRuleException("Document type and number must either both be provided or both be omitted.");
        }

        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public string CustomerCode { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public EmailAddress Email { get; private set; } = null!;
    public string EmailNormalized { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? DocumentType { get; private set; }
    public string? DocumentNumber { get; private set; }
    public string? DocumentNumberNormalized { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public static Customer Create(string code, string firstName, string lastName, string email,
        string? phone, string? documentType, string? documentNumber, DateTimeOffset nowUtc) =>
        new(Guid.NewGuid(), NexusPay.Domain.Customers.CustomerCode.Create(code), firstName, lastName,
            EmailAddress.Create(email), phone, documentType, documentNumber, nowUtc);

    public void Update(string firstName, string lastName, string email, string? phone,
        string? documentType, string? documentNumber, DateTimeOffset nowUtc)
    {
        if (!IsActive) throw new DomainRuleException("An inactive customer cannot be updated.");
        FirstName = RequiredName(firstName, nameof(firstName));
        LastName = RequiredName(lastName, nameof(lastName));
        Email = EmailAddress.Create(email);
        EmailNormalized = Email.NormalizedValue;
        Phone = NormalizeOptional(phone, 32, nameof(phone));
        DocumentType = NormalizeOptional(documentType, 32, nameof(documentType));
        DocumentNumber = NormalizeOptional(documentNumber, 64, nameof(documentNumber));
        DocumentNumberNormalized = DocumentNumber?.ToUpperInvariant();
        if ((DocumentType is null) != (DocumentNumber is null))
            throw new DomainRuleException("Document type and number must either both be provided or both be omitted.");
        UpdatedAtUtc = nowUtc.ToUniversalTime();
    }

    public void Deactivate(DateTimeOffset nowUtc)
    {
        if (!IsActive) return;
        IsActive = false;
        UpdatedAtUtc = nowUtc.ToUniversalTime();
    }

    public void Activate(DateTimeOffset nowUtc)
    {
        if (IsActive) return;
        IsActive = true;
        UpdatedAtUtc = nowUtc.ToUniversalTime();
    }

    private static string RequiredName(string value, string field)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > 100)
            throw new DomainRuleException($"{field} is required and must be at most 100 characters.");
        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string field)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        if (normalized.Length > maxLength) throw new DomainRuleException($"{field} must be at most {maxLength} characters.");
        return normalized;
    }
}
