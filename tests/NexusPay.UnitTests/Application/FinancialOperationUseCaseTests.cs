using FluentValidation;
using NexusPay.Application.Auditing;
using NexusPay.Application.Common;
using NexusPay.Application.Customers;
using NexusPay.Application.Operations;
using NexusPay.Domain.Common;
using NexusPay.Domain.Customers;
using NexusPay.Domain.Operations;
using NexusPay.Domain.Providers;

namespace NexusPay.UnitTests.Application;

public sealed class FinancialOperationUseCaseTests
{
    [Fact]
    public async Task CreateOperation_SameKeyAndPayload_ReturnsOriginalWithoutDuplicate()
    {
        var fixture = new Fixture();
        var request = fixture.Request("create-key", 45m);

        var first = await fixture.CreateUseCase().ExecuteAsync(request);
        var repeated = await fixture.CreateUseCase().ExecuteAsync(request with { Currency = "pen" });

        Assert.Equal(first.Id, repeated.Id);
        Assert.Equal(1, fixture.Operations.AddCount);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateOperation_SameKeyWithDifferentPayload_Conflicts()
    {
        var fixture = new Fixture();
        await fixture.CreateUseCase().ExecuteAsync(fixture.Request("create-key", 45m));

        await Assert.ThrowsAsync<ConflictException>(() =>
            fixture.CreateUseCase().ExecuteAsync(fixture.Request("create-key", 46m)));
        Assert.Equal(1, fixture.Operations.AddCount);
    }

    [Fact]
    public async Task ProcessOperation_UsesSimulatorAndRepeatingKeyDoesNotProcessAgain()
    {
        var fixture = new Fixture();
        var created = await fixture.CreateUseCase().ExecuteAsync(fixture.Request("create-key", 45m));
        fixture.Simulator.Result = new PaymentSimulationResult(PaymentSimulationOutcome.Approved, "SIM-REF-01");
        var useCase = fixture.ProcessUseCase();

        var first = await useCase.ExecuteAsync(created.Id, new ProcessFinancialOperationRequest("payment-key"));
        var repeated = await useCase.ExecuteAsync(created.Id, new ProcessFinancialOperationRequest("payment-key"));

        Assert.Equal(OperationStatus.Completed, first.Status);
        Assert.Equal("SIM-REF-01", first.ProviderReference);
        Assert.Equal(first, repeated);
        Assert.Equal(1, fixture.Simulator.CallCount);
        Assert.Equal(3, fixture.AuditLogs.Entries.Count);
        Assert.Single(fixture.IntegrationLogs.Entries);
    }

    [Theory]
    [InlineData(PaymentSimulationOutcome.Rejected, OperationStatus.Failed)]
    [InlineData(PaymentSimulationOutcome.TechnicalError, OperationStatus.Failed)]
    [InlineData(PaymentSimulationOutcome.Timeout, OperationStatus.Failed)]
    public async Task ProcessOperation_MapsNonApprovalSimulationToFailed(PaymentSimulationOutcome outcome, OperationStatus expected)
    {
        var fixture = new Fixture();
        var created = await fixture.CreateUseCase().ExecuteAsync(fixture.Request("create-key", 1m));
        fixture.Simulator.Result = new PaymentSimulationResult(outcome);

        var result = await fixture.ProcessUseCase().ExecuteAsync(created.Id,
            new ProcessFinancialOperationRequest("payment-key"));

        Assert.Equal(expected, result.Status);
        Assert.Equal(1, result.ProcessingAttempts);
        Assert.NotNull(result.FailureCode);
    }

    private sealed class Fixture
    {
        public readonly FakeUnitOfWork UnitOfWork = new();
        public readonly FakeOperations Operations = new();
        public readonly FakeSimulator Simulator = new();
        public readonly FakeProviders Providers = new();
        public readonly FakeAuditLogs AuditLogs = new();
        public readonly FakeIntegrationLogs IntegrationLogs = new();
        private readonly FakeCustomers _customers = new();
        private readonly FixedClock _clock = new();

        public CreateFinancialOperationRequest Request(string key, decimal amount) => new(
            _customers.Customer.Id, OperationType.Payment, amount, "PEN", "Simulated operation", key);

        public CreateFinancialOperationUseCase CreateUseCase() => new(new CreateFinancialOperationValidator(),
            _customers, Operations, new FixedCodeGenerator(), AuditLogs, UnitOfWork, _clock);

        public ProcessFinancialOperationUseCase ProcessUseCase() => new(new ProcessFinancialOperationValidator(),
            Operations, Providers, Simulator, UnitOfWork, _clock, AuditLogs, IntegrationLogs);
    }

    private sealed class FakeCustomers : ICustomerRepository
    {
        public Customer Customer { get; } = TestData.Customer();
        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Customer?>(id == Customer.Id ? Customer : null);
        public Task AddAsync(Customer customer, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<(IReadOnlyList<Customer> Items, int TotalCount)> SearchAsync(string? search, bool? isActive,
            int page, int pageSize, string sortBy, string sortDirection, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Customer>, int)>(([], 0));
    }

    private sealed class FakeOperations : IFinancialOperationRepository
    {
        private readonly Dictionary<Guid, FinancialOperation> _byId = [];
        private readonly Dictionary<string, FinancialOperation> _byKey = new(StringComparer.Ordinal);
        public int AddCount { get; private set; }
        public Task<FinancialOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.GetValueOrDefault(id));
        public Task<FinancialOperation?> GetByIdempotencyKeyAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byKey.GetValueOrDefault(key));
        public Task AddAsync(FinancialOperation operation, CancellationToken cancellationToken = default)
        {
            AddCount++;
            _byId.Add(operation.Id, operation);
            _byKey.Add(operation.IdempotencyKey, operation);
            return Task.CompletedTask;
        }
        public Task AddPaymentAttemptAsync(PaymentAttempt attempt, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
        public Task<(IReadOnlyList<FinancialOperation> Items, int TotalCount)> SearchAsync(Guid? customerId,
            OperationStatus? status, string? currency, DateTimeOffset? from, DateTimeOffset? to,
            int page, int pageSize, string sortBy, string sortDirection, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<FinancialOperation>, int)>(([], 0));
    }

    private sealed class FakeProviders : IPaymentProviderRepository
    {
        private readonly PaymentProvider _provider = PaymentProvider.Create(
            Guid.Parse("c6e7d319-868b-4a11-929d-a51d6d0d5e10"), "NexusProviderAlpha", "Nexus Provider Alpha",
            DateTimeOffset.Parse("2026-01-02T03:04:05Z"));
        public Task<PaymentProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<PaymentProvider?>(id == _provider.Id ? _provider : null);
        public Task<PaymentProvider?> GetByCodeAsync(string providerCode, CancellationToken cancellationToken = default) =>
            Task.FromResult<PaymentProvider?>(string.Equals(providerCode, _provider.ProviderCode, StringComparison.OrdinalIgnoreCase) ? _provider : null);
        public Task<IReadOnlyList<PaymentProvider>> GetEnabledAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PaymentProvider>>([_provider]);
        public Task AddAsync(PaymentProvider provider, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeAuditLogs : IAuditLogRepository
    {
        public List<NexusPay.Domain.Auditing.AuditLog> Entries { get; } = [];
        public Task AddAsync(NexusPay.Domain.Auditing.AuditLog entry, CancellationToken cancellationToken = default)
        { Entries.Add(entry); return Task.CompletedTask; }
    }

    private sealed class FakeIntegrationLogs : IIntegrationLogRepository
    {
        public List<NexusPay.Domain.Integrations.IntegrationLog> Entries { get; } = [];
        public Task AddAsync(NexusPay.Domain.Integrations.IntegrationLog entry, CancellationToken cancellationToken = default)
        { Entries.Add(entry); return Task.CompletedTask; }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        { SaveCount++; return Task.FromResult(1); }
    }

    private sealed class FixedClock : IClock { public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-01-02T03:04:05Z"); }
    private sealed class FixedCodeGenerator : IOperationCodeGenerator { public string NextCode(DateTimeOffset nowUtc) => "OP-20260102030405"; }

    private sealed class FakeSimulator : IPaymentSimulator
    {
        public Guid ProviderId => Guid.Parse("c6e7d319-868b-4a11-929d-a51d6d0d5e10");
        public string ProviderCode => "NexusProviderAlpha";
        public int CallCount { get; private set; }
        public PaymentSimulationResult Result { get; set; } = new(PaymentSimulationOutcome.Approved, "SIM-REF-01");
        public Task<PaymentSimulationResult> ProcessAsync(PaymentSimulationRequest request, CancellationToken cancellationToken = default)
        { CallCount++; return Task.FromResult(Result); }
    }
}

