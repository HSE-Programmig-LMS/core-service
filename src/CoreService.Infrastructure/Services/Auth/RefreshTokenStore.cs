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

    public async Task<ActiveRefreshToken?> GetActiveAsync(string rawRefreshToken, CancellationToken ct = default)
    {
        var hash = HashToken(rawRefreshToken);
        var now = _clock.UtcNow;

        var token = await _db.RefreshTokens
            .AsNoTracking()
            .Where(t => t.TokenHash == hash && t.RevokedAt == null && t.ExpiresAt > now)
            .FirstOrDefaultAsync(ct);

        return token is null ? null : new ActiveRefreshToken(token.UserId, token.ExpiresAt);
    }

    public async Task<bool> RotateAsync(
        string oldRawRefreshToken,
        string newRawRefreshToken,
        DateTimeOffset newExpiresAtUtc,
        CancellationToken ct = default)
    {
        var oldHash = HashToken(oldRawRefreshToken);
        var now = _clock.UtcNow;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var oldToken = await _db.RefreshTokens
            .Where(t => t.TokenHash == oldHash && t.RevokedAt == null && t.ExpiresAt > now)
            .FirstOrDefaultAsync(ct);

        if (oldToken is null)
        {
            await tx.RollbackAsync(ct);
            return false;
        }

        // revoke old
        oldToken.RevokedAt = now;

        // insert new
        var newEntity = new RefreshToken
        {
            TokenId = Guid.NewGuid(),
            UserId = oldToken.UserId,
            TokenHash = HashToken(newRawRefreshToken),
            ExpiresAt = newExpiresAtUtc,
            RevokedAt = null
        };

        _db.RefreshTokens.Add(newEntity);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return true;
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
        var bytes = Encoding.UTF8.GetBytes(raw);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash); // uppercase hex
    }
}