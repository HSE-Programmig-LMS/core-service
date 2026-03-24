using CoreService.Application.Contracts.Users;

namespace CoreService.Application.Abstractions.Users;

public interface IUserRepository
{
    /// <summary>
    /// Проверка существования email
    /// </summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Получить пользователя по id. Возвращает null если не найден
    /// </summary>
    Task<UserDto?> GetByIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Получить список пользователей с фильтрами/пагинацией.
    /// </summary>
    Task<PagedResult<UserDto>> GetListAsync(UsersQuery query, CancellationToken ct = default);

    /// <summary>
    /// Создать пользователя без назначения роли
    /// </summary>
    Task<UserDto> CreateAsync(CreateUserData data, CancellationToken ct = default);

    /// <summary>
    /// Обновить базовые поля пользователя
    /// Возвращает null если пользователь не найден.
    /// </summary>
    Task<UserDto?> UpdateAsync(Guid userId, UpdateUserData data, CancellationToken ct = default);

    /// <summary>
    /// Деактивировать пользователя.
    /// Возвращает false если пользователь не найден.
    /// </summary>
    Task<bool> DeactivateAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Получить роль пользователя, если назначена.
    /// Возвращает null, если роль не назначена/пользователь не найден.
    /// </summary>
    Task<string?> GetUserRoleAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Установить роль пользователя
    /// Возвращает false если пользователь не найден.
    /// </summary>
    Task<bool> SetUserRoleAsync(Guid userId, string roleCode, CancellationToken ct = default);
}

/// <summary>
/// Данные для создания пользователя
/// </summary>
public sealed record CreateUserData(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    bool IsActive);

/// <summary>
/// Данные для обновления пользователя. Null означает "не менять".
/// </summary>
public sealed record UpdateUserData(
    string? Email,
    string? FirstName,
    string? LastName,
    bool? IsActive);
