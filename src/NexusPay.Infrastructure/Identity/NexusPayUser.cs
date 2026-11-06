using Microsoft.AspNetCore.Identity;

namespace NexusPay.Infrastructure.Identity;

public sealed class NexusPayUser : IdentityUser
{
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
