using Microsoft.AspNetCore.Identity;

namespace CoreService.Infrastructure.Identity;

public sealed class ApplicationUserRole : IdentityUserRole<Guid>
{
    public Guid UserRoleId { get; set; } = Guid.NewGuid();
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
}
