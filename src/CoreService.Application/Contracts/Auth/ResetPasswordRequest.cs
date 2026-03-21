namespace CoreService.Application.Contracts.Auth;

public sealed record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword);