namespace NexusPay.Application.Common;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
