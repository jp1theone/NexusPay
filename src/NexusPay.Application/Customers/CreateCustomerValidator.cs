using FluentValidation;

namespace NexusPay.Application.Customers;

public sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.CustomerCode).NotEmpty().MaximumLength(32).Matches("^[A-Za-z0-9][A-Za-z0-9-]{2,31}$");
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
