using Microsoft.AspNetCore.Identity;

namespace CoreService.Infrastructure.Identity;

public sealed class ApplicationUserRole : IdentityUserRole<Guid>
{
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
}
