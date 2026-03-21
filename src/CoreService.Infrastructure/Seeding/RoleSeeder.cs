using CoreService.Domain.Security;
using CoreService.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CoreService.Infrastructure.Seeding;

public sealed class RoleSeeder
{
    private readonly RoleManager<ApplicationRole> _roleManager;

    public RoleSeeder(RoleManager<ApplicationRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        foreach (var code in RoleCodeMapper.All)
        {
            var codeStr = RoleCodeMapper.ToDb(code);
            var normalized = codeStr.ToUpperInvariant();

            var exists = await _roleManager.FindByNameAsync(codeStr);
            if (exists is not null) continue;

            var role = new ApplicationRole
            {
                RoleCode = code,
                RoleName = RoleCodeMapper.ToDisplay(code),

                // Identity внутренне опирается на Name/NormalizedName
                Name = codeStr,
                NormalizedName = normalized
            };

            var res = await _roleManager.CreateAsync(role);
            if (!res.Succeeded)
                throw new InvalidOperationException("Role seeding failed: " + string.Join("; ", res.Errors.Select(e => e.Description)));
        }
    }
}
