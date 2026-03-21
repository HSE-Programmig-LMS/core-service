using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Common.Errors;
using CoreService.Application.Common.Results;
using CoreService.Application.Contracts.Auth;

namespace CoreService.Application.UseCases.Auth;

public sealed class LogoutUseCase
{
    private readonly IRefreshTokenStore _refreshTokenStore;

    public LogoutUseCase(IRefreshTokenStore refreshTokenStore)
    {
        _refreshTokenStore = refreshTokenStore;
    }

    public async Task<Result<bool>> ExecuteAsync(RefreshRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result<bool>.Fail(
                AppError.Validation("Validation failed.",
                    new Dictionary<string, string[]> { ["RefreshToken"] = new[] { "RefreshToken is required." } }));
        }

        // Logout делаем идемпотентным: даже если токена нет — OK.
        await _refreshTokenStore.RevokeAsync(request.RefreshToken, ct);
        return Result<bool>.Ok(true);
    }
}