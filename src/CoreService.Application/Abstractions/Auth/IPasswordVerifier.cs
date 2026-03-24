namespace CoreService.Application.Abstractions.Auth;

/// <summary>
/// Абстракция проверки email+password.
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
/// Если IsValid = false, Code содержит причину
/// </summary>
public sealed record PasswordVerificationResult(
    bool IsValid,
    Guid? UserId,
    string? Email,
    string? RoleCode,
    string? FirstName,
    string? LastName,
    string? FailureCode);
