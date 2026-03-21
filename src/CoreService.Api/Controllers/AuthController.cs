using CoreService.Api.Common;
using CoreService.Application.Contracts.Auth;
using CoreService.Application.UseCases.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreService.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly LoginUseCase _login;
    private readonly RefreshUseCase _refresh;
    private readonly LogoutUseCase _logout;
    private readonly ForgotPasswordUseCase _forgotPassword;
    private readonly ResetPasswordUseCase _resetPassword;

    public AuthController(
        LoginUseCase login,
        RefreshUseCase refresh,
        LogoutUseCase logout,
        ForgotPasswordUseCase forgotPassword,
        ResetPasswordUseCase resetPassword)
    {
        _login = login;
        _refresh = refresh;
        _logout = logout;
        _forgotPassword = forgotPassword;
        _resetPassword = resetPassword;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _login.ExecuteAsync(request, ct);
        return result.ToActionResult(this);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var result = await _refresh.ExecuteAsync(request, ct);
        return result.ToActionResult(this);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var result = await _logout.ExecuteAsync(request, ct);
        return result.ToActionResult(this);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        var result = await _forgotPassword.ExecuteAsync(request, ct);
        return result.ToActionResult(this);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await _resetPassword.ExecuteAsync(request, ct);
        return result.ToActionResult(this);
    }
}