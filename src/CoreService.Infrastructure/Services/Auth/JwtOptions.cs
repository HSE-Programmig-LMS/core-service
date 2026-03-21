namespace CoreService.Infrastructure.Services.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "";
    public string Audience { get; init; } = "";
    public string SigningKey { get; init; } = "";

    /// <summary>Срок жизни access token в минутах.</summary>
    public int AccessTokenLifetimeMinutes { get; init; } = 15;
}
