namespace CoreService.Application.Contracts.Users;

/// <summary>
/// Параметры поиска пользователей для списка.
/// </summary>
public sealed record UsersQuery(
    string? EmailContains = null,
    string? Role = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 20);