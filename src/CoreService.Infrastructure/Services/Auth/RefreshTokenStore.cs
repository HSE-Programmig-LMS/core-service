using System.Security.Cryptography;
using System.Text;
using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Abstractions.Common;
using CoreService.Infrastructure.Persistence;
using CoreService.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreService.Infrastructure.Services.Auth;

public sealed class RefreshTokenStore : IRefreshTokenStore
{
    private readonly CoreDbContext _db;
    private readonly IClock _clock;

    public RefreshTokenStore(CoreDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task StoreAsync(
        Guid userId,
        string rawRefreshToken,
        DateTimeOffset expiresAtUtc,
        CancellationToken ct = default)
    {
        var hash = HashToken(rawRefreshToken);

        var entity = new RefreshToken
        {
            TokenId = Guid.NewGuid(),
            UserId = userId,
            TokenHash = hash,
            ExpiresAt = expiresAtUtc,
            RevokedAt = null
        };

        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RevokeAsync(string rawRefreshToken, CancellationToken ct = default)
    {
        var hash = HashToken(rawRefreshToken);

        var token = await _db.RefreshTokens
            .Where(t => t.TokenHash == hash && t.RevokedAt == null)
            .OrderByDescending(t => t.ExpiresAt)
            .FirstOrDefaultAsync(ct);

        if (token is null) return;

        token.RevokedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private static string HashToken(string raw)
    {
        // SHA-256(base64url_token) -> hex string
        var bytes = Encoding.UTF8.GetBytes(raw);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash); // uppercase hex
    }
}
