using FluentValidation;

namespace NexusPay.Application.Operations;

public sealed class CreateFinancialOperationValidator : AbstractValidator<CreateFinancialOperationRequest>
{
    public CreateFinancialOperationValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(9_999_999_999_999_999.99m)
            .Must(amount => decimal.Round(amount, 2) == amount)
            .WithMessage("Amount must fit decimal(18,2) and have at most two decimal places.");
        RuleFor(x => x.Currency).NotEmpty().Must(currency => new[] { "PEN", "USD", "EUR" }
            .Contains(currency.Trim(), StringComparer.OrdinalIgnoreCase)).WithMessage("Currency must be PEN, USD or EUR.");
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}
