namespace CoreService.Application.Contracts.Users;

/// <summary>
/// Отдельный контракт на смену роли.
/// Можно использовать вместо Role в UpdateUserRequest, если хочешь более явный API.
/// </summary>
public sealed record ChangeUserRoleRequest(
    string Role);