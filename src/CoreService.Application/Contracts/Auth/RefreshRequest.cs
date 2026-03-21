namespace CoreService.Application.Contracts.Auth;

public sealed record RefreshRequest(
    string RefreshToken);
