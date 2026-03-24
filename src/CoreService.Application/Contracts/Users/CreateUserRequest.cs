namespace CoreService.Application.Contracts.Users;

/// <summary>
/// Создание пользователя менеджером.
/// Role — обязательна
/// </summary>
public sealed record CreateUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive = true);
