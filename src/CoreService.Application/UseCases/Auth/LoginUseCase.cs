using System.Security.Cryptography;
using System.Text;
using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Abstractions.Common;
using CoreService.Application.Common.Errors;
using CoreService.Application.Common.Results;
using CoreService.Application.Contracts.Auth;

namespace CoreService.Application.UseCases.Auth;

public sealed class LoginUseCase
{
    private readonly IPasswordVerifier _passwordVerifier;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IClock _clock;
    private readonly TimeSpan _refreshTokenLifetime;

    public LoginUseCase(
        IPasswordVerifier passwordVerifier,
        IJwtTokenService jwtTokenService,
        IRefreshTokenStore refreshTokenStore,
        IClock clock,
        TimeSpan refreshTokenLifetime)
    {
        _passwordVerifier = passwordVerifier;
        _jwtTokenService = jwtTokenService;
        _refreshTokenStore = refreshTokenStore;
        _clock = clock;
        _refreshTokenLifetime = refreshTokenLifetime;
    }

    public async Task<Result<LoginResponse>> ExecuteAsync(LoginRequest request, CancellationToken ct = default)
    {
        // Basic validation
        var errors = Validate(request);
        if (errors is not null)
            return Result<LoginResponse>.Fail(AppError.Validation("Validation failed.", errors));

        var verification = await _passwordVerifier.VerifyAsync(request.Email, request.Password, ct);

        if (!verification.IsValid)
        {
            // Minimal leakage: we map specific reasons to generic messages where appropriate.
            return verification.FailureCode switch
            {
                ErrorCodes.UserInactive => Result<LoginResponse>.Fail(AppError.UserInactive()),
                ErrorCodes.LockedOut => Result<LoginResponse>.Fail(AppError.LockedOut()),
                _ => Result<LoginResponse>.Fail(AppError.InvalidCredentials())
            };
        }

        // Should be present when IsValid == true
        var userId = verification.UserId!.Value;
        var email = verification.Email!;
        var role = verification.RoleCode!;

        var access = await _jwtTokenService.CreateAccessTokenAsync(userId, role, email, ct);

        var refreshRaw = GenerateSecureToken(64); // 64 bytes -> base64url string
        var refreshExpiresAt = _clock.UtcNow.Add(_refreshTokenLifetime);

        await _refreshTokenStore.StoreAsync(userId, refreshRaw, refreshExpiresAt, ct);

        var response = new LoginResponse(
            UserId: userId,
            Email: email,
            Role: role,
            AccessToken: access.AccessToken,
            AccessTokenExpiresAtUtc: access.ExpiresAtUtc,
            RefreshToken: refreshRaw,
            RefreshTokenExpiresAtUtc: refreshExpiresAt
        );

        return Result<LoginResponse>.Ok(response);
    }

    private static IReadOnlyDictionary<string, string[]>? Validate(LoginRequest request)
    {
        var dict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.Email))
            Add(dict, nameof(request.Email), "Email is required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            Add(dict, nameof(request.Password), "Password is required.");

        return dict.Count == 0
            ? null
            : dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
    }

    private static void Add(Dictionary<string, List<string>> dict, string key, string error)
    {
        if (!dict.TryGetValue(key, out var list))
        {
            list = new List<string>();
            dict[key] = list;
        }
        list.Add(error);
    }

    /// <summary>
    /// Генерация криптографически стойкого токена (base64url).
    /// </summary>
    private static string GenerateSecureToken(int byteLength)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] data)
    {
        // Base64url without padding
        var s = Convert.ToBase64String(data);
        s = s.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return s;
    }
}
