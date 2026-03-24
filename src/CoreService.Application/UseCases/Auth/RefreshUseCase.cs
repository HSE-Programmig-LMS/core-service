using System.Security.Cryptography;
using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Abstractions.Common;
using CoreService.Application.Abstractions.Users;
using CoreService.Application.Common.Errors;
using CoreService.Application.Common.Results;
using CoreService.Application.Contracts.Auth;

namespace CoreService.Application.UseCases.Auth;

public sealed class RefreshUseCase
{
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IUserRepository _users;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IClock _clock;
    private readonly TimeSpan _refreshTokenLifetime;

    public RefreshUseCase(
        IRefreshTokenStore refreshTokenStore,
        IUserRepository users,
        IJwtTokenService jwtTokenService,
        IClock clock,
        TimeSpan refreshTokenLifetime)
    {
        _refreshTokenStore = refreshTokenStore;
        _users = users;
        _jwtTokenService = jwtTokenService;
        _clock = clock;
        _refreshTokenLifetime = refreshTokenLifetime;
    }

    public async Task<Result<RefreshResponse>> ExecuteAsync(RefreshRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result<RefreshResponse>.Fail(
                AppError.Validation("Validation failed.",
                    new Dictionary<string, string[]> { ["RefreshToken"] = new[] { "RefreshToken is required." } }));
        }

        // Проверяем, что refresh токен активен
        var active = await _refreshTokenStore.GetActiveAsync(request.RefreshToken, ct);
        if (active is null)
            return Result<RefreshResponse>.Fail(new AppError(ErrorCodes.InvalidRefreshToken, "Invalid refresh token."));

        // Загружаем пользователя и роль (для JWT)
        var user = await _users.GetByIdAsync(active.UserId, ct);
        if (user is null || !user.IsActive)
            return Result<RefreshResponse>.Fail(AppError.Unauthorized());

        // Генерим access
        var access = await _jwtTokenService.CreateAccessTokenAsync(user.UserId, user.Role, user.Email, ct);

        // Генерим новый refresh
        var newRefreshRaw = GenerateSecureToken(64);
        var newRefreshExpiresAt = _clock.UtcNow.Add(_refreshTokenLifetime);

        // Rotation
        var rotated = await _refreshTokenStore.RotateAsync(
            oldRawRefreshToken: request.RefreshToken,
            newRawRefreshToken: newRefreshRaw,
            newExpiresAtUtc: newRefreshExpiresAt,
            ct: ct);

        if (!rotated)
            return Result<RefreshResponse>.Fail(new AppError(ErrorCodes.InvalidRefreshToken, "Invalid refresh token."));

        return Result<RefreshResponse>.Ok(new RefreshResponse(
            AccessToken: access.AccessToken,
            AccessTokenExpiresAtUtc: access.ExpiresAtUtc,
            RefreshToken: newRefreshRaw,
            RefreshTokenExpiresAtUtc: newRefreshExpiresAt
        ));
    }

    private static string GenerateSecureToken(int byteLength)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        var s = Convert.ToBase64String(bytes);
        // base64url without padding
        return s.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
