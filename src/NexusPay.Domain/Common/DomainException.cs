namespace NexusPay.Domain.Common;

public class DomainException(string message) : Exception(message);

public sealed class DomainRuleException(string message) : DomainException(message);

public sealed class ConflictException(string message) : DomainException(message);
