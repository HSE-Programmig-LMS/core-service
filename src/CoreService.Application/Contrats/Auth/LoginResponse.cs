namespace CoreService.Application.Contracts.Auth;

public sealed record LoginResponse(
    Guid UserId,
    string Email,
    string Role,                 // role_code: student/assistant/teacher/manager
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);
