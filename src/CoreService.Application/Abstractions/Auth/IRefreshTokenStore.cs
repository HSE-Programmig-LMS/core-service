namespace CoreService.Application.Abstractions.Auth;

/// <summary>
/// Хранилище refresh токенов. Application передаёт "сырой" refresh token,
/// а инфраструктура должна сохранить его безопасно (например, хранить только hash).
/// </summary>
public interface IRefreshTokenStore
{
    Task StoreAsync(
        Guid userId,
        string rawRefreshToken,
        DateTimeOffset expiresAtUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Отозвать refresh token (например, при logout).
    /// Реализация может искать по hash.
    /// </summary>
    Task RevokeAsync(
        string rawRefreshToken,
        CancellationToken ct = default);
}
