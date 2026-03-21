namespace CoreService.Application.Contracts.Users;

/// <summary>
/// Частичное обновление пользователя.
/// Null означает "не менять".
/// Role опциональна: если задана — меняем роль (1 роль на пользователя).
/// </summary>
public sealed record UpdateUserRequest(
    string? Email = null,
    string? FirstName = null,
    string? LastName = null,
    string? Role = null,
    bool? IsActive = null);