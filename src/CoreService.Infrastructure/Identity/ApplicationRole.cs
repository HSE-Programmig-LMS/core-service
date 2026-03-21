using CoreService.Domain.Security;
using Microsoft.AspNetCore.Identity;

namespace CoreService.Infrastructure.Identity;

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public RoleCode RoleCode { get; set; }
    public string RoleName { get; set; } = "";
}
