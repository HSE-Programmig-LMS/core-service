namespace CoreService.Application.Abstractions.Auth;

/// <summary>
/// Контекст текущего пользователя (из JWT/HTTP контекста).
/// Реализация будет в API/Infrastructure.
/// </summary>
public interface IUserContext
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string? RoleCode { get; }
    string? Email { get; }
}
