using NexusPay.Domain.Operations;

namespace NexusPay.Application.Operations;

internal static class FinancialOperationMapper
{
    public static FinancialOperationDto ToDto(FinancialOperation operation) => new(
        operation.Id, operation.OperationCode, operation.CustomerId, operation.Type,
        operation.Money.Amount, operation.Money.Currency, operation.Status,
        operation.Description, operation.ProcessingAttempts, operation.LastProviderCode,
        operation.LastProviderReference, operation.LastFailureCode, operation.CreatedAtUtc,
        operation.ProcessedAtUtc);
}
