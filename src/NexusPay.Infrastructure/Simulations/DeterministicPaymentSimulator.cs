using NexusPay.Application.Operations;

namespace NexusPay.Infrastructure.Simulations;

public sealed class DeterministicPaymentSimulator(PaymentSimulationOutcome outcome, string providerCode = "NexusProviderAlpha")
    : IPaymentSimulator
{
    public static readonly Guid DefaultProviderId = Guid.Parse("c6e7d319-868b-4a11-929d-a51d6d0d5e10");
    public Guid ProviderId => DefaultProviderId;
    public string ProviderCode { get; } = string.IsNullOrWhiteSpace(providerCode)
        ? throw new ArgumentException("A provider code is required.", nameof(providerCode))
        : providerCode.Trim().ToUpperInvariant();

    public Task<PaymentSimulationResult> ProcessAsync(PaymentSimulationRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var reference = $"SIM-{request.OperationId.ToString("N")[..12].ToUpperInvariant()}";
        var result = outcome switch
        {
            PaymentSimulationOutcome.Approved => new PaymentSimulationResult(outcome, reference),
            PaymentSimulationOutcome.Rejected => new PaymentSimulationResult(outcome, FailureCode: "SIMULATED_REJECTION"),
            PaymentSimulationOutcome.TechnicalError => new PaymentSimulationResult(outcome, FailureCode: "SIMULATED_TECHNICAL_ERROR"),
            PaymentSimulationOutcome.Timeout => new PaymentSimulationResult(outcome, FailureCode: "SIMULATED_TIMEOUT"),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), "Unsupported simulated outcome.")
        };
        return Task.FromResult(result);
    }
}
