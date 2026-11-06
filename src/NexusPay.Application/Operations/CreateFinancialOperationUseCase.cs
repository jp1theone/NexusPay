using FluentValidation;
using NexusPay.Application.Common;
using NexusPay.Application.Auditing;
using NexusPay.Domain.Auditing;
using NexusPay.Application.Customers;
using NexusPay.Domain.Common;
using NexusPay.Domain.Operations;

namespace NexusPay.Application.Operations;

public sealed class CreateFinancialOperationUseCase(
    IValidator<CreateFinancialOperationRequest> validator,
    ICustomerRepository customers,
    IFinancialOperationRepository operations,
    IOperationCodeGenerator codeGenerator,
    IAuditLogRepository auditLogs,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<(FinancialOperationDto Operation, bool IsReplay)> ExecuteWithResultAsync(
        CreateFinancialOperationRequest request, string? actorUserId = null, string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var customer = await customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new DomainRuleException("Customer was not found.");
        var candidate = FinancialOperation.Create(codeGenerator.NextCode(clock.UtcNow), customer,
            request.Type, Money.Create(request.Amount, request.Currency), request.Description,
            request.IdempotencyKey, clock.UtcNow);

        var existing = await operations.GetByIdempotencyKeyAsync(candidate.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!existing.HasSameRequest(candidate.RequestHash))
                throw new ConflictException("Idempotency key was already used with a different request.");
            return (FinancialOperationMapper.ToDto(existing), true);
        }

        await operations.AddAsync(candidate, cancellationToken);
        await auditLogs.AddAsync(AuditLog.Create(actorUserId, "FinancialOperationCreated", "FinancialOperation",
            candidate.Id.ToString("D"), clock.UtcNow, correlationId ?? Guid.NewGuid().ToString("N"),
            "Succeeded", $"Created {candidate.OperationCode} for customer {candidate.CustomerId:D}."), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (FinancialOperationMapper.ToDto(candidate), false);
    }

    public async Task<FinancialOperationDto> ExecuteAsync(CreateFinancialOperationRequest request,
        string? actorUserId = null, string? correlationId = null, CancellationToken cancellationToken = default) =>
        (await ExecuteWithResultAsync(request, actorUserId, correlationId, cancellationToken)).Operation;
}
