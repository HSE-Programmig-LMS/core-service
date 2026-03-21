using CoreService.Application.Abstractions.Audit;
using CoreService.Infrastructure.Persistence;
using CoreService.Infrastructure.Persistence.Entities;

namespace CoreService.Infrastructure.Services.Audit;

public sealed class AuditWriter : IAuditWriter
{
    private readonly CoreDbContext _db;

    public AuditWriter(CoreDbContext db)
    {
        _db = db;
    }

    public async Task WriteAsync(AuditWriteEntry entry, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entry.EventType))
            throw new ArgumentException("EventType is required.", nameof(entry));

        if (string.IsNullOrWhiteSpace(entry.EntityType))
            throw new ArgumentException("EntityType is required.", nameof(entry));

        var entity = new AuditEvent
        {
            EventId = entry.EventId ?? Guid.NewGuid(),
            EventType = entry.EventType,
            ActorUserId = entry.ActorUserId,
            CreatedAt = entry.CreatedAtUtc ?? DateTimeOffset.UtcNow,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            Details = string.IsNullOrWhiteSpace(entry.DetailsJson) ? "{}" : entry.DetailsJson
        };

        _db.AuditEvents.Add(entity);
        await _db.SaveChangesAsync(ct);
    }
}