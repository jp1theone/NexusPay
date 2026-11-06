using NexusPay.Application.Operations;

namespace NexusPay.Infrastructure;

public sealed class OperationCodeGenerator : IOperationCodeGenerator
{
    public string NextCode(DateTimeOffset nowUtc) => $"NXP-{nowUtc.UtcDateTime:yyyyMMddHHmmss}-{Guid.NewGuid():N}".ToUpperInvariant();
}
