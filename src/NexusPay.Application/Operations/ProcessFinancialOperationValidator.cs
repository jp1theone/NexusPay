using FluentValidation;

namespace NexusPay.Application.Operations;

public sealed class ProcessFinancialOperationValidator : AbstractValidator<ProcessFinancialOperationRequest>
{
    public ProcessFinancialOperationValidator() => RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(128);
}
