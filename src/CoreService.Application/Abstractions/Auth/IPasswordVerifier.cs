namespace CoreService.Application.Abstractions.Auth;

/// <summary>
/// Абстракция проверки email+password.
/// Реализация будет в инфраструктуре (Identity UserManager/SignInManager),
/// чтобы Application не зависел от Identity.
/// </summary>
public interface IPasswordVerifier
{
    Task<PasswordVerificationResult> VerifyAsync(
        string email,
        string password,
        CancellationToken ct = default);
}

/// <summary>
/// Результат проверки пароля.
/// Если IsValid = false, Code содержит причину (например invalid_credentials/locked_out/user_inactive).
/// </summary>
public sealed record PasswordVerificationResult(
    bool IsValid,
    Guid? UserId,
    string? Email,
    string? RoleCode,
    string? FirstName,
    string? LastName,
    string? FailureCode);
