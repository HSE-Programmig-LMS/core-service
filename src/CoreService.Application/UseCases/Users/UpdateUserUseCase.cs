using System.Text.Json;
using CoreService.Application.Abstractions.Audit;
using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Abstractions.Users;
using CoreService.Application.Common.Errors;
using CoreService.Application.Common.Results;
using CoreService.Application.Contracts.Users;
using CoreService.Domain.Audit;

namespace CoreService.Application.UseCases.Users;

public sealed class UpdateUserUseCase
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IAuditWriter _audit;
    private readonly IUserContext _userContext;

    public UpdateUserUseCase(IUserRepository users, IRoleRepository roles, IAuditWriter audit, IUserContext userContext)
    {
        _users = users;
        _roles = roles;
        _audit = audit;
        _userContext = userContext;
    }

    public async Task<Result<UserDto>> ExecuteAsync(Guid userId, UpdateUserRequest request, CancellationToken ct = default)
    {
        if (request.Email is null && request.FirstName is null && request.LastName is null && request.IsActive is null && request.Role is null)
        {
            return Result<UserDto>.Fail(AppError.Validation("Validation failed.",
                new Dictionary<string, string[]> { ["request"] = ["At least one field must be provided."] }));
        }

        var current = await _users.GetByIdAsync(userId, ct);
        if (current is null)
            return Result<UserDto>.Fail(new AppError(ErrorCodes.UserNotFound, "User not found."));

        // Email uniqueness check
        if (!string.IsNullOrWhiteSpace(request.Email) &&
            !string.Equals(request.Email, current.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (await _users.EmailExistsAsync(request.Email, ct))
                return Result<UserDto>.Fail(new AppError(ErrorCodes.EmailAlreadyExists, "Email already exists."));
        }

        string? normalizedRole = null;
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            normalizedRole = _roles.NormalizeRoleCode(request.Role);
            if (normalizedRole is null || !await _roles.ExistsAsync(normalizedRole, ct))
                return Result<UserDto>.Fail(new AppError(ErrorCodes.RoleNotFound, "Role not found."));
        }

        var updated = await _users.UpdateAsync(userId,
            new UpdateUserData(
                Email: request.Email,
                FirstName: request.FirstName,
                LastName: request.LastName,
                IsActive: request.IsActive
            ),
            ct);

        if (updated is null)
            return Result<UserDto>.Fail(new AppError(ErrorCodes.UserNotFound, "User not found."));

        // Role change
        if (normalizedRole is not null && !string.Equals(current.Role, normalizedRole, StringComparison.OrdinalIgnoreCase))
        {
            var ok = await _users.SetUserRoleAsync(userId, normalizedRole, ct);
            if (!ok)
                return Result<UserDto>.Fail(new AppError(ErrorCodes.UserNotFound, "User not found."));

            await _audit.WriteAsync(new AuditWriteEntry(
                EventType: AuditEventTypes.CoreUserRoleChanged,
                ActorUserId: _userContext.UserId,
                EntityType: AuditEntityTypes.User,
                EntityId: userId,
                DetailsJson: JsonSerializer.Serialize(new { old_role = current.Role, new_role = normalizedRole })
            ), ct);
        }

        // Reload to ensure role
        var finalUser = await _users.GetByIdAsync(userId, ct);
        if (finalUser is null)
            return Result<UserDto>.Fail(AppError.Internal("User updated but cannot be loaded."));

        await _audit.WriteAsync(new AuditWriteEntry(
            EventType: AuditEventTypes.CoreUserUpdated,
            ActorUserId: _userContext.UserId,
            EntityType: AuditEntityTypes.User,
            EntityId: userId,
            DetailsJson: JsonSerializer.Serialize(new
            {
                old = new { email = current.Email, first_name = current.FirstName, last_name = current.LastName, is_active = current.IsActive },
                @new = new { email = finalUser.Email, first_name = finalUser.FirstName, last_name = finalUser.LastName, is_active = finalUser.IsActive }
            })
        ), ct);

        return Result<UserDto>.Ok(finalUser);
    }
}
