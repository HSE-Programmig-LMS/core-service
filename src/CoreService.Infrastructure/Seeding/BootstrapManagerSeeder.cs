using CoreService.Domain.Security;
using CoreService.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace CoreService.Infrastructure.Seeding;

public sealed class BootstrapManagerSeeder
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<ApplicationRole> _roles;

    public BootstrapManagerSeeder(
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles)
    {
        _users = users;
        _roles = roles;
    }

    public async Task SeedIfConfiguredAsync(IConfiguration cfg, CancellationToken ct = default)
    {
        var email = cfg["Bootstrap:Manager:Email"];
        var password = cfg["Bootstrap:Manager:Password"];
        var firstName = cfg["Bootstrap:Manager:FirstName"] ?? "Менеджер";
        var lastName  = cfg["Bootstrap:Manager:LastName"]  ?? "Дисциплины";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return; 

        // если пользователь уже есть — выходим
        var existing = await _users.FindByEmailAsync(email);
        if (existing is not null) return;

        // убеждаемся, что роль менеджера существует
        var managerRoleName = RoleCodeMapper.ToDb(RoleCode.Manager); // "manager"
        var role = await _roles.FindByNameAsync(managerRoleName);
        if (role is null)
            throw new InvalidOperationException($"Role '{managerRoleName}' is missing. Run RoleSeeder first.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            EmailConfirmed = true
        };

        var create = await _users.CreateAsync(user, password);
        if (!create.Succeeded)
            throw new InvalidOperationException("Bootstrap manager create failed: " +
                string.Join("; ", create.Errors.Select(e => e.Description)));

        var addRole = await _users.AddToRoleAsync(user, managerRoleName);
        if (!addRole.Succeeded)
            throw new InvalidOperationException("Bootstrap manager role bind failed: " +
                string.Join("; ", addRole.Errors.Select(e => e.Description)));
    }
}