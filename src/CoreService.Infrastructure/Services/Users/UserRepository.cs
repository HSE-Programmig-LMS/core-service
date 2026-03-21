using CoreService.Application.Abstractions.Users;
using CoreService.Application.Contracts.Users;
using CoreService.Infrastructure.Identity;
using CoreService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CoreService.Infrastructure.Services.Users;

public sealed class UserRepository : IUserRepository
{
    private readonly CoreDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserRepository(CoreDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        var normalized = _userManager.NormalizeEmail(email);
        return await _db.Users.AnyAsync(u => u.NormalizedEmail == normalized, ct);
    }

    public async Task<UserDto?> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        // join: users -> user_roles -> roles
        var query =
            from u in _db.Users.AsNoTracking()
            join ur in _db.Set<ApplicationUserRole>().AsNoTracking()
                on u.Id equals ur.UserId into urj
            from ur in urj.DefaultIfEmpty()
            join r in _db.Roles.AsNoTracking()
                on ur.RoleId equals r.Id into rj
            from r in rj.DefaultIfEmpty()
            where u.Id == userId
            select new UserDto(
                UserId: u.Id,
                Email: u.Email ?? "",
                FirstName: u.FirstName,
                LastName: u.LastName,
                IsActive: u.IsActive,
                Role: r != null ? (r.Name ?? "") : "",
                CreatedAtUtc: u.CreatedAt,
                LastLoginAtUtc: u.LastLoginAt
            );

        return await query.FirstOrDefaultAsync(ct);
    }

    public async Task<PagedResult<UserDto>> GetListAsync(UsersQuery query, CancellationToken ct = default)
    {
        // Важно: у вас 1 роль на пользователя, поэтому join не раздувает строки
        var baseQuery =
            from u in _db.Users.AsNoTracking()
            join ur in _db.Set<ApplicationUserRole>().AsNoTracking()
                on u.Id equals ur.UserId into urj
            from ur in urj.DefaultIfEmpty()
            join r in _db.Roles.AsNoTracking()
                on ur.RoleId equals r.Id into rj
            from r in rj.DefaultIfEmpty()
            select new { u, roleName = (string?)r!.Name };

        if (!string.IsNullOrWhiteSpace(query.EmailContains))
        {
            var part = query.EmailContains.Trim().ToLowerInvariant();
            baseQuery = baseQuery.Where(x => (x.u.Email ?? "").ToLower().Contains(part));
        }

        if (query.IsActive.HasValue)
            baseQuery = baseQuery.Where(x => x.u.IsActive == query.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var role = query.Role.Trim().ToLowerInvariant();
            baseQuery = baseQuery.Where(x => (x.roleName ?? "").ToLower() == role);
        }

        // TotalCount
        var total = await baseQuery.LongCountAsync(ct);

        // Order + pagination
        var page = query.Page;
        var size = query.PageSize;

        var items = await baseQuery
            .OrderBy(x => x.u.Email) // можно поменять на CreatedAt desc
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => new UserDto(
                UserId: x.u.Id,
                Email: x.u.Email ?? "",
                FirstName: x.u.FirstName,
                LastName: x.u.LastName,
                IsActive: x.u.IsActive,
                Role: x.roleName ?? "",
                CreatedAtUtc: x.u.CreatedAt,
                LastLoginAtUtc: x.u.LastLoginAt
            ))
            .ToListAsync(ct);

        return new PagedResult<UserDto>(items, page, size, total);
    }

    public async Task<UserDto> CreateAsync(CreateUserData data, CancellationToken ct = default)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = data.Email,
            UserName = data.Email, // удобно использовать email как username
            FirstName = data.FirstName,
            LastName = data.LastName,
            IsActive = data.IsActive,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var res = await _userManager.CreateAsync(user, data.Password);
        if (!res.Succeeded)
        {
            var msg = string.Join("; ", res.Errors.Select(e => e.Code + ": " + e.Description));
            throw new InvalidOperationException("User creation failed: " + msg);
        }

        // role на этом шаге может быть пустым (назначается use-case’ом отдельно)
        return new UserDto(
            UserId: user.Id,
            Email: user.Email ?? "",
            FirstName: user.FirstName,
            LastName: user.LastName,
            IsActive: user.IsActive,
            Role: "",
            CreatedAtUtc: user.CreatedAt,
            LastLoginAtUtc: user.LastLoginAt
        );
    }

    public async Task<UserDto?> UpdateAsync(Guid userId, UpdateUserData data, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return null;

        // Email change
        if (!string.IsNullOrWhiteSpace(data.Email) &&
            !string.Equals(data.Email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            // UserName = Email (упрощаем)
            var setEmail = await _userManager.SetEmailAsync(user, data.Email);
            if (!setEmail.Succeeded)
                throw new InvalidOperationException("SetEmail failed: " + string.Join("; ", setEmail.Errors.Select(e => e.Description)));

            var setUserName = await _userManager.SetUserNameAsync(user, data.Email);
            if (!setUserName.Succeeded)
                throw new InvalidOperationException("SetUserName failed: " + string.Join("; ", setUserName.Errors.Select(e => e.Description)));
        }

        if (data.FirstName is not null) user.FirstName = data.FirstName;
        if (data.LastName is not null) user.LastName = data.LastName;
        if (data.IsActive.HasValue) user.IsActive = data.IsActive.Value;

        var res = await _userManager.UpdateAsync(user);
        if (!res.Succeeded)
            throw new InvalidOperationException("User update failed: " + string.Join("; ", res.Errors.Select(e => e.Description)));

        // вернём актуальную DTO (с ролью через join)
        return await GetByIdAsync(user.Id, ct);
    }

    public async Task<bool> DeactivateAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return false;

        user.IsActive = false;

        var res = await _userManager.UpdateAsync(user);
        if (!res.Succeeded)
            throw new InvalidOperationException("User deactivate failed: " + string.Join("; ", res.Errors.Select(e => e.Description)));

        return true;
    }

    public async Task<string?> GetUserRoleAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return roles.Count > 0 ? roles[0] : null;
    }

    public async Task<bool> SetUserRoleAsync(Guid userId, string roleCode, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return false;

        // У нас 1 роль на пользователя: удаляем все и добавляем новую
        var current = await _userManager.GetRolesAsync(user);
        if (current.Count > 0)
        {
            var remove = await _userManager.RemoveFromRolesAsync(user, current);
            if (!remove.Succeeded)
                throw new InvalidOperationException("RemoveFromRoles failed: " + string.Join("; ", remove.Errors.Select(e => e.Description)));
        }

        var add = await _userManager.AddToRoleAsync(user, roleCode);
        if (!add.Succeeded)
            throw new InvalidOperationException("AddToRole failed: " + string.Join("; ", add.Errors.Select(e => e.Description)));

        return true;
    }
}