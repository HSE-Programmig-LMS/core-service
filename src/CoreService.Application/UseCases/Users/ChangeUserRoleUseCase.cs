using System.Text.Json;
using CoreService.Application.Abstractions.Audit;
using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Abstractions.Users;
using CoreService.Application.Common.Errors;
using CoreService.Application.Common.Results;
using CoreService.Application.Contracts.Users;
using CoreService.Domain.Audit;

namespace CoreService.Application.UseCases.Users;

public sealed class ChangeUserRoleUseCase
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IAuditWriter _audit;
    private readonly IUserContext _userContext;

    public ChangeUserRoleUseCase(
        IUserRepository users,
        IRoleRepository roles,
        IAuditWriter audit,
        IUserContext userContext)
    {
        _users = users;
        _roles = roles;
        _audit = audit;
        _userContext = userContext;
    }

    public async Task<Result<UserDto>> ExecuteAsync(Guid userId, ChangeUserRoleRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Role))
            return Result<UserDto>.Fail(AppError.Validation("Validation failed.",
                new Dictionary<string, string[]> { ["Role"] = ["Role is required."] }));

        var newRole = _roles.NormalizeRoleCode(request.Role);
        if (newRole is null || !await _roles.ExistsAsync(newRole, ct))
            return Result<UserDto>.Fail(new AppError(ErrorCodes.RoleNotFound, "Role not found."));

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
            return Result<UserDto>.Fail(new AppError(ErrorCodes.UserNotFound, "User not found."));

        var oldRole = user.Role;
        if (string.Equals(oldRole, newRole, StringComparison.OrdinalIgnoreCase))
            return Result<UserDto>.Ok(user); // no-op

        var ok = await _users.SetUserRoleAsync(userId, newRole, ct);
        if (!ok)
            return Result<UserDto>.Fail(new AppError(ErrorCodes.UserNotFound, "User not found."));

        var updated = await _users.GetByIdAsync(userId, ct);
        if (updated is null)
            return Result<UserDto>.Fail(AppError.Internal("User role updated but cannot be loaded."));

        await _audit.WriteAsync(new AuditWriteEntry(
            EventType: AuditEventTypes.CoreUserRoleChanged,
            ActorUserId: _userContext.UserId,
            EntityType: AuditEntityTypes.User,
            EntityId: userId,
            DetailsJson: JsonSerializer.Serialize(new { old_role = oldRole, new_role = newRole })
        ), ct);

        return Result<UserDto>.Ok(updated);
    }
}