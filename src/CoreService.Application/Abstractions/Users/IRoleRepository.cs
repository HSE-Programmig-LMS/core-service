namespace CoreService.Application.Abstractions.Users;

public interface IRoleRepository
{
    /// <summary>
    /// Проверить, что роль с данным role_code существует
    /// </summary>
    Task<bool> ExistsAsync(string roleCode, CancellationToken ct = default);

    /// <summary>
    /// Нормализовать/валидационно привести role_code к каноническому виду.
    /// Возвращает null, если значение некорректно.
    /// </summary>
    string? NormalizeRoleCode(string? roleCode);
}
