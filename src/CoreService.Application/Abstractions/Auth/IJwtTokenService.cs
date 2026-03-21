namespace CoreService.Application.Abstractions.Auth;

public interface IJwtTokenService
{
    /// <summary>
    /// Создать access JWT для пользователя.
    /// </summary>
    Task<AccessTokenResult> CreateAccessTokenAsync(
        Guid userId,
        string roleCode,
        string email,
        CancellationToken ct = default);
}

/// <summary>
/// Результат выпуска access token.
/// </summary>
public sealed record AccessTokenResult(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);
