namespace CoreService.Application.Contracts.Auth;

public sealed record ForgotPasswordRequest(
    string Email);