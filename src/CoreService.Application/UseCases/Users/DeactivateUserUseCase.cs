using System.Text.Json;
using CoreService.Application.Abstractions.Audit;
using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Abstractions.Users;
using CoreService.Application.Common.Errors;
using CoreService.Application.Common.Results;
using CoreService.Domain.Audit;

namespace CoreService.Application.UseCases.Users;

public sealed class DeactivateUserUseCase
{
    private readonly IUserRepository _users;
    private readonly IAuditWriter _audit;
    private readonly IUserContext _userContext;

    public DeactivateUserUseCase(IUserRepository users, IAuditWriter audit, IUserContext userContext)
    {
        _users = users;
        _audit = audit;
        _userContext = userContext;
    }

    public async Task<Result<bool>> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        var ok = await _users.DeactivateAsync(userId, ct);
        if (!ok)
            return Result<bool>.Fail(new AppError(ErrorCodes.UserNotFound, "User not found."));

        await _audit.WriteAsync(new AuditWriteEntry(
            EventType: AuditEventTypes.CoreUserDeactivated,
            ActorUserId: _userContext.UserId,
            EntityType: AuditEntityTypes.User,
            EntityId: userId,
            DetailsJson: JsonSerializer.Serialize(new { is_active = false })
        ), ct);

        return Result<bool>.Ok(true);
    }
}