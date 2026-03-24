using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Common.Errors;
using CoreService.Application.Common.Results;
using CoreService.Application.Contracts.Auth;

namespace CoreService.Application.UseCases.Auth;

public sealed class ForgotPasswordUseCase
{
    private readonly IPasswordResetService _passwordResetService;

    public ForgotPasswordUseCase(IPasswordResetService passwordResetService)
    {
        _passwordResetService = passwordResetService;
    }

    public async Task<Result<bool>> ExecuteAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Result<bool>.Fail(
                AppError.Validation("Validation failed.",
                    new Dictionary<string, string[]> { ["Email"] = new[] { "Email is required." } }));
        }

        // Если email не найден — вернётся null, но наружу это не показываем.
        _ = await _passwordResetService.GenerateResetTokenAsync(request.Email, ct);

        return Result<bool>.Ok(true);
    }
}
