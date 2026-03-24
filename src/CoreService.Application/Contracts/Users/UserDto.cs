namespace CoreService.Application.Contracts.Users;

/// <summary>
/// Публичное представление пользователя для API.
/// Role — role_code (student/assistant/teacher/manager).
/// </summary>
public sealed record UserDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive,
    string Role,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc);
