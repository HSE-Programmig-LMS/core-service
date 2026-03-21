namespace CoreService.Infrastructure.Persistence.Entities;

public sealed class RefreshToken
{
    public Guid TokenId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = "";

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
