namespace CoreService.Application.Abstractions.Auth;

/// <summary>
/// Контекст текущего пользователя
/// </summary>
public interface IUserContext
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string? RoleCode { get; }
    string? Email { get; }
}
