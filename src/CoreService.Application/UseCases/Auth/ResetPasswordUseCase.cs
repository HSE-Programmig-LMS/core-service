using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Common.Errors;
using CoreService.Application.Common.Results;
using CoreService.Application.Contracts.Auth;

namespace CoreService.Application.UseCases.Auth;

public sealed class ResetPasswordUseCase
{
    private readonly IPasswordResetService _passwordResetService;

    public ResetPasswordUseCase(IPasswordResetService passwordResetService)
    {
        _passwordResetService = passwordResetService;
    }

    public async Task<Result<bool>> ExecuteAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var errors = Validate(request);
        if (errors is not null)
            return Result<bool>.Fail(AppError.Validation("Validation failed.", errors));

        var result = await _passwordResetService.ResetPasswordAsync(
            request.Email, request.Token, request.NewPassword, ct);

        if (result.Succeeded)
            return Result<bool>.Ok(true);

        // Не раскрываем детали (например, "email не существует")
        // Возвращаем общий код ошибки.
        return Result<bool>.Fail(new AppError(
            Code: ErrorCodes.InvalidResetToken,
            Message: "Password reset failed."
        ));
    }

    private static IReadOnlyDictionary<string, string[]>? Validate(ResetPasswordRequest r)
    {
        var d = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(r.Email)) Add(d, nameof(r.Email), "Email is required.");
        if (string.IsNullOrWhiteSpace(r.Token)) Add(d, nameof(r.Token), "Token is required.");
        if (string.IsNullOrWhiteSpace(r.NewPassword)) Add(d, nameof(r.NewPassword), "NewPassword is required.");

        return d.Count == 0
            ? null
            : d.ToDictionary(k => k.Key, v => v.Value.ToArray(), StringComparer.OrdinalIgnoreCase);

        static void Add(Dictionary<string, List<string>> dict, string key, string err)
        {
            if (!dict.TryGetValue(key, out var list))
            {
                list = new List<string>();
                dict[key] = list;
            }
            list.Add(err);
        }
    }
}