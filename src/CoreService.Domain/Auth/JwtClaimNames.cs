namespace CoreService.Domain.Auth;

/// <summary>
/// Claim name for JWT token
/// </summary>
public static class JwtClaimNames
{
    public const string Subject = "sub";
    public const string Role = "role";   // role code as string
    public const string Email = "email";
}
