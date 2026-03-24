using System.Text.Json;
using CoreService.Application.Abstractions.Audit;
using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Abstractions.Users;
using CoreService.Application.Common.Errors;
using CoreService.Application.Common.Results;
using CoreService.Application.Contracts.Users;
using CoreService.Domain.Audit;

namespace CoreService.Application.UseCases.Users;

public sealed class CreateUserUseCase
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IAuditWriter _audit;
    private readonly IUserContext _userContext;

    public CreateUserUseCase(
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

    public async Task<Result<UserDto>> ExecuteAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var validation = Validate(request);
        if (validation is not null)
            return Result<UserDto>.Fail(AppError.Validation("Validation failed.", validation));

        var roleCode = _roles.NormalizeRoleCode(request.Role);
        if (roleCode is null)
            return Result<UserDto>.Fail(new AppError(ErrorCodes.RoleNotFound, "Role is invalid."));

        if (!await _roles.ExistsAsync(roleCode, ct))
            return Result<UserDto>.Fail(new AppError(ErrorCodes.RoleNotFound, "Role not found."));

        if (await _users.EmailExistsAsync(request.Email, ct))
            return Result<UserDto>.Fail(new AppError(ErrorCodes.EmailAlreadyExists, "Email already exists."));

        var created = await _users.CreateAsync(
            new CreateUserData(
                Email: request.Email,
                Password: request.Password,
                FirstName: request.FirstName,
                LastName: request.LastName,
                IsActive: request.IsActive
            ),
            ct);

        var setRoleOk = await _users.SetUserRoleAsync(created.UserId, roleCode, ct);
        if (!setRoleOk)
            return Result<UserDto>.Fail(AppError.Internal("Failed to assign role."));

        var finalUser = await _users.GetByIdAsync(created.UserId, ct);
        if (finalUser is null)
            return Result<UserDto>.Fail(AppError.Internal("User created but cannot be loaded."));

        var actorId = _userContext.UserId;

        // core.user.created
        await _audit.WriteAsync(new AuditWriteEntry(
            EventType: AuditEventTypes.CoreUserCreated,
            ActorUserId: actorId,
            EntityType: AuditEntityTypes.User,
            EntityId: finalUser.UserId,
            DetailsJson: Json(new
            {
                email = finalUser.Email,
                first_name = finalUser.FirstName,
                last_name = finalUser.LastName,
                is_active = finalUser.IsActive
            })
        ), ct);

        // core.user.role.changed
        await _audit.WriteAsync(new AuditWriteEntry(
            EventType: AuditEventTypes.CoreUserRoleChanged,
            ActorUserId: actorId,
            EntityType: AuditEntityTypes.User,
            EntityId: finalUser.UserId,
            DetailsJson: Json(new
            {
                old_role = (string?)null,
                new_role = roleCode
            })
        ), ct);

        return Result<UserDto>.Ok(finalUser);
    }

    private static IReadOnlyDictionary<string, string[]>? Validate(CreateUserRequest r)
    {
        var d = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(r.Email)) Add(d, nameof(r.Email), "Email is required.");
        if (string.IsNullOrWhiteSpace(r.Password)) Add(d, nameof(r.Password), "Password is required.");
        if (string.IsNullOrWhiteSpace(r.FirstName)) Add(d, nameof(r.FirstName), "FirstName is required.");
        if (string.IsNullOrWhiteSpace(r.LastName)) Add(d, nameof(r.LastName), "LastName is required.");
        if (string.IsNullOrWhiteSpace(r.Role)) Add(d, nameof(r.Role), "Role is required.");

        return d.Count == 0
            ? null
            : d.ToDictionary(k => k.Key, v => v.Value.ToArray(), StringComparer.OrdinalIgnoreCase);

        static void Add(Dictionary<string, List<string>> dict, string key, string err)
        {
            if (!dict.TryGetValue(key, out var list))
            {
                list = new List<string>();
                dict[key] = list;
            }
            list.Add(err);
        }
    }

    private static string Json(object obj) =>
        JsonSerializer.Serialize(obj, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
}
