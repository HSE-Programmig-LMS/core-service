using CoreService.Application.Abstractions.Auth;
using CoreService.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CoreService.Infrastructure.Services.Auth;

public sealed class PasswordResetService : IPasswordResetService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public PasswordResetService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<string?> GenerateResetTokenAsync(string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null) return null;

        if (!user.IsActive) return null;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        return token;
    }

    public async Task<PasswordResetResult> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(token) ||
            string.IsNullOrWhiteSpace(newPassword))
        {
            return new PasswordResetResult(false, "validation_error", "Invalid input.");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
        {
            return new PasswordResetResult(false, "invalid_reset_token", "Invalid token.");
        }

        var res = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (res.Succeeded)
            return new PasswordResetResult(true);

        return new PasswordResetResult(false, "invalid_reset_token", "Reset failed.");
    }
}
