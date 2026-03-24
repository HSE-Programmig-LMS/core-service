namespace CoreService.Application.Abstractions.Auth;

/// <summary>
/// Хранилище refresh токенов. Application передаёт "сырой" refresh token,
/// а инфраструктура должна сохранить его безопасно
/// </summary>
public interface IRefreshTokenStore
{
    Task StoreAsync(
        Guid userId,
        string rawRefreshToken,
        DateTimeOffset expiresAtUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Возвращает запись токена, если он активен 
    /// </summary>
    Task<ActiveRefreshToken?> GetActiveAsync(
        string rawRefreshToken,
        CancellationToken ct = default);

    /// <summary>
    /// Rotation: атомарно отозвать старый refresh token и создать новый.
    /// Возвращает false, если старый токен не активен.
    /// </summary>
    Task<bool> RotateAsync(
        string oldRawRefreshToken,
        string newRawRefreshToken,
        DateTimeOffset newExpiresAtUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Отозвать refresh token
    /// Операция идемпотентна: если токен не найден — просто ничего не делает.
    /// </summary>
    Task RevokeAsync(
        string rawRefreshToken,
        CancellationToken ct = default);
}

/// <summary>
/// Минимальная информация об активном refresh токене.
/// </summary>
public sealed record ActiveRefreshToken(
    Guid UserId,
    DateTimeOffset ExpiresAtUtc);
