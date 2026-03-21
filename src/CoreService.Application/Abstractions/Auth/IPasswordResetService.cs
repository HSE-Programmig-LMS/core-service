namespace CoreService.Application.Abstractions.Auth;

/// <summary>
/// Абстракция для механизма сброса пароля (на базе Identity, но Application об этом не знает).
/// </summary>
public interface IPasswordResetService
{
    /// <summary>
    /// Возвращает токен сброса пароля, если пользователь существует и может сбрасывать пароль.
    /// Если пользователя нет/неактивен — возвращает null (чтобы UseCase мог не "палить" существование).
    /// </summary>
    Task<string?> GenerateResetTokenAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Пытается сбросить пароль по email+token.
    /// </summary>
    Task<PasswordResetResult> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken ct = default);
}

/// <summary>
/// Результат операции reset password.
/// </summary>
public sealed record PasswordResetResult(
    bool Succeeded,
    string? FailureCode = null,
    string? FailureMessage = null);