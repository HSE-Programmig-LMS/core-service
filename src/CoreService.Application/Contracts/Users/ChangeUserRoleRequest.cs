namespace CoreService.Application.Contracts.Users;

/// <summary>
/// Отдельный контракт на смену роли.
/// </summary>
public sealed record ChangeUserRoleRequest(
    string Role);
