using FluentValidation;
using NexusPay.Application.Auditing;
using NexusPay.Application.Common;
using NexusPay.Domain.Common;
using NexusPay.Domain.Auditing;
using NexusPay.Domain.Integrations;
using NexusPay.Domain.Operations;

namespace NexusPay.Application.Operations;

public sealed class ProcessFinancialOperationUseCase(
    IValidator<ProcessFinancialOperationRequest> validator,
    IFinancialOperationRepository operations,
    IPaymentProviderRepository providers,
    IPaymentSimulator simulator,
    IUnitOfWork unitOfWork,
    IClock clock,
    IAuditLogRepository auditLogs,
    IIntegrationLogRepository integrationLogs)
{
    public async Task<FinancialOperationDto> ExecuteAsync(Guid operationId,
        ProcessFinancialOperationRequest request, string? actorUserId = null,
        string? correlationId = null, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var operation = await operations.GetByIdAsync(operationId, cancellationToken)
            ?? throw new DomainRuleException("Operation was not found.");

        if (string.Equals(operation.LastProcessIdempotencyKey, request.IdempotencyKey.Trim().ToUpperInvariant(), StringComparison.Ordinal))
            return FinancialOperationMapper.ToDto(operation);

        var provider = await providers.GetByCodeAsync(simulator.ProviderCode, cancellationToken);
        if (provider is null || !provider.IsEnabled || !provider.IsAvailable)
            throw new DomainRuleException("The simulated payment provider is not enabled and available.");

        var effectiveCorrelationId = string.IsNullOrWhiteSpace(correlationId)
            ? Guid.NewGuid().ToString("N") : correlationId.Trim();

        operation.StartProcessing(request.IdempotencyKey, simulator.ProviderId, simulator.ProviderCode,
            clock.UtcNow, actorUserId: actorUserId, correlationId: effectiveCorrelationId);
        await operations.AddPaymentAttemptAsync(operation.PaymentAttempts.Last(), cancellationToken);
        await auditLogs.AddAsync(AuditLog.Create(actorUserId, "PaymentProcessingStarted", "FinancialOperation",
            operation.Id.ToString("D"), clock.UtcNow, effectiveCorrelationId, "Succeeded"), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var startedAtUtc = clock.UtcNow;
        PaymentSimulationResult result;
        try
        {
            result = await simulator.ProcessAsync(new PaymentSimulationRequest(operation.Id, operation.Type,
                operation.Money.Amount, operation.Money.Currency, operation.LastProcessIdempotencyKey!), cancellationToken);
        }
        catch (TimeoutException)
        {
            result = new PaymentSimulationResult(PaymentSimulationOutcome.Timeout, FailureCode: "SIMULATED_TIMEOUT");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            result = new PaymentSimulationResult(PaymentSimulationOutcome.TechnicalError,
                FailureCode: "SIMULATED_TECHNICAL_ERROR");
        }

        var attempt = operation.PaymentAttempts.Last();
        var integrationOutcome = result.Outcome switch
        {
            PaymentSimulationOutcome.Approved => IntegrationOutcome.Succeeded,
            PaymentSimulationOutcome.Rejected => IntegrationOutcome.Rejected,
            PaymentSimulationOutcome.Timeout => IntegrationOutcome.TimedOut,
            _ => IntegrationOutcome.Failed
        };
        await integrationLogs.AddAsync(IntegrationLog.Create(simulator.ProviderId, attempt.Id,
            effectiveCorrelationId, integrationOutcome, result.FailureCode,
            Math.Max(0, (int)(clock.UtcNow - startedAtUtc).TotalMilliseconds), clock.UtcNow), cancellationToken);
        switch (result.Outcome)
        {
            case PaymentSimulationOutcome.Approved:
                operation.Complete(result.ProviderReference, clock.UtcNow, actorUserId, effectiveCorrelationId);
                break;
            case PaymentSimulationOutcome.Rejected:
                operation.Fail(result.FailureCode ?? "SIMULATED_REJECTION", clock.UtcNow, PaymentAttemptStatus.Rejected, actorUserId, effectiveCorrelationId);
                break;
            case PaymentSimulationOutcome.TechnicalError:
                operation.Fail(result.FailureCode ?? "SIMULATED_TECHNICAL_ERROR", clock.UtcNow, actorUserId: actorUserId, correlationId: effectiveCorrelationId);
                break;
            case PaymentSimulationOutcome.Timeout:
                operation.Fail(result.FailureCode ?? "SIMULATED_TIMEOUT", clock.UtcNow, PaymentAttemptStatus.TimedOut, actorUserId, effectiveCorrelationId);
                break;
            default:
                throw new DomainRuleException("The payment simulator returned an unsupported outcome.");
        }

        await auditLogs.AddAsync(AuditLog.Create(actorUserId, "PaymentProcessingCompleted", "FinancialOperation",
            operation.Id.ToString("D"), clock.UtcNow, effectiveCorrelationId,
            operation.Status == OperationStatus.Completed ? "Succeeded" : "Failed"), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return FinancialOperationMapper.ToDto(operation);
    }
}
