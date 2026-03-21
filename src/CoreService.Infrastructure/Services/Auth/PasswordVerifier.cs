using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Common.Errors;
using CoreService.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CoreService.Infrastructure.Services.Auth;

public sealed class PasswordVerifier : IPasswordVerifier
{
    private readonly UserManager<ApplicationUser> _userManager;

    public PasswordVerifier(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<PasswordVerificationResult> VerifyAsync(
        string email,
        string password,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return new PasswordVerificationResult(
                IsValid: false,
                UserId: null,
                Email: null,
                RoleCode: null,
                FirstName: null,
                LastName: null,
                FailureCode: ErrorCodes.InvalidCredentials);
        }

        // Identity нормализует email сам, но FindByEmailAsync ожидает "как есть"
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            // Не раскрываем факт существования пользователя
            return new PasswordVerificationResult(
                IsValid: false,
                UserId: null,
                Email: null,
                RoleCode: null,
                FirstName: null,
                LastName: null,
                FailureCode: ErrorCodes.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return new PasswordVerificationResult(
                IsValid: false,
                UserId: user.Id,
                Email: user.Email,
                RoleCode: null,
                FirstName: user.FirstName,
                LastName: user.LastName,
                FailureCode: ErrorCodes.UserInactive);
        }

        // Lockout check
        if (await _userManager.IsLockedOutAsync(user))
        {
            return new PasswordVerificationResult(
                IsValid: false,
                UserId: user.Id,
                Email: user.Email,
                RoleCode: null,
                FirstName: user.FirstName,
                LastName: user.LastName,
                FailureCode: ErrorCodes.LockedOut);
        }

        var ok = await _userManager.CheckPasswordAsync(user, password);
        if (!ok)
        {
            // увеличиваем счётчик ошибок (может привести к lockout)
            await _userManager.AccessFailedAsync(user);

            // если после увеличения он залочился — вернём locked_out
            if (await _userManager.IsLockedOutAsync(user))
            {
                return new PasswordVerificationResult(
                    IsValid: false,
                    UserId: user.Id,
                    Email: user.Email,
                    RoleCode: null,
                    FirstName: user.FirstName,
                    LastName: user.LastName,
                    FailureCode: ErrorCodes.LockedOut);
            }

            return new PasswordVerificationResult(
                IsValid: false,
                UserId: user.Id,
                Email: user.Email,
                RoleCode: null,
                FirstName: user.FirstName,
                LastName: user.LastName,
                FailureCode: ErrorCodes.InvalidCredentials);
        }

        // успешный вход: сбрасываем счётчик ошибок
        await _userManager.ResetAccessFailedCountAsync(user);

        // получаем роль (по нашему правилу — одна роль)
        var roles = await _userManager.GetRolesAsync(user);
        var roleCode = roles.Count == 1 ? roles[0] : null;

        if (string.IsNullOrWhiteSpace(roleCode))
        {
            // конфигурационная ошибка (роль не назначена)
            return new PasswordVerificationResult(
                IsValid: false,
                UserId: user.Id,
                Email: user.Email,
                RoleCode: null,
                FirstName: user.FirstName,
                LastName: user.LastName,
                FailureCode: ErrorCodes.InternalError);
        }

        return new PasswordVerificationResult(
            IsValid: true,
            UserId: user.Id,
            Email: user.Email ?? email,
            RoleCode: roleCode,
            FirstName: user.FirstName,
            LastName: user.LastName,
            FailureCode: null);
    }
}
