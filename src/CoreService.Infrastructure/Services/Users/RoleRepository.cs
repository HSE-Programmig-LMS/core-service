using CoreService.Application.Abstractions.Users;
using CoreService.Domain.Security;
using CoreService.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CoreService.Infrastructure.Services.Users;

public sealed class RoleRepository : IRoleRepository
{
    private readonly RoleManager<ApplicationRole> _roleManager;

    public RoleRepository(RoleManager<ApplicationRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task<bool> ExistsAsync(string roleCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(roleCode)) return false;
        var normalized = NormalizeRoleCode(roleCode);
        if (normalized is null) return false;

        // Identity ищет по Name (мы храним Name="manager"/"student" и т.п.)
        var role = await _roleManager.FindByNameAsync(normalized);
        return role is not null;
    }

    public string? NormalizeRoleCode(string? roleCode)
    {
        if (string.IsNullOrWhiteSpace(roleCode)) return null;

        try
        {
            // Приводим к каноническому виду через доменный маппер
            var parsed = RoleCodeMapper.FromDb(roleCode);
            return RoleCodeMapper.ToDb(parsed);
        }
        catch
        {
            return null;
        }
    }
}