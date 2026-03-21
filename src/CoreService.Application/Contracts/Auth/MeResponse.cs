namespace CoreService.Application.Contracts.Auth;

public sealed record MeResponse(
    Guid UserId,
    string Email,
    string Role,
    string FirstName,
    string LastName);
