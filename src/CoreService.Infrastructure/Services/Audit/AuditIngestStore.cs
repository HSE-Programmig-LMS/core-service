using CoreService.Application.Abstractions.Audit;
using CoreService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreService.Infrastructure.Services.Audit;

public sealed class AuditIngestStore : IAuditIngestStore
{
    private readonly CoreDbContext _db;

    public AuditIngestStore(CoreDbContext db)
    {
        _db = db;
    }

    public async Task<int> InsertIgnoreDuplicatesAsync(
        IReadOnlyList<AuditWriteEntry> events,
        CancellationToken ct = default)
    {
        if (events.Count == 0) return 0;

        var sql = new System.Text.StringBuilder();
        var parameters = new List<object>();

        sql.Append("INSERT INTO audit_events (event_id, event_type, actor_user_id, created_at, entity_type, entity_id, details) VALUES ");

        for (int i = 0; i < events.Count; i++)
        {
            var e = events[i];

            var pEventId = new Npgsql.NpgsqlParameter($"p_event_id_{i}", e.EventId ?? Guid.NewGuid());
            var pEventType = new Npgsql.NpgsqlParameter($"p_event_type_{i}", e.EventType);
            var pActor = new Npgsql.NpgsqlParameter($"p_actor_{i}", (object?)e.ActorUserId ?? DBNull.Value);
            var pCreated = new Npgsql.NpgsqlParameter($"p_created_{i}", (object?)(e.CreatedAtUtc ?? DateTimeOffset.UtcNow) ?? DBNull.Value);
            var pEntityType = new Npgsql.NpgsqlParameter($"p_entity_type_{i}", e.EntityType);
            var pEntityId = new Npgsql.NpgsqlParameter($"p_entity_id_{i}", (object?)e.EntityId ?? DBNull.Value);
            var pDetails = new Npgsql.NpgsqlParameter($"p_details_{i}", string.IsNullOrWhiteSpace(e.DetailsJson) ? "{}" : e.DetailsJson);

            parameters.AddRange(new object[] { pEventId, pEventType, pActor, pCreated, pEntityType, pEntityId, pDetails });

            sql.Append($"(@{pEventId.ParameterName}, @{pEventType.ParameterName}, @{pActor.ParameterName}, @{pCreated.ParameterName}, @{pEntityType.ParameterName}, @{pEntityId.ParameterName}, @{pDetails.ParameterName})");
            if (i < events.Count - 1) sql.Append(", ");
        }

        sql.Append(" ON CONFLICT (event_id) DO NOTHING;");

        var affected = await _db.Database.ExecuteSqlRawAsync(sql.ToString(), parameters.ToArray(), ct);
        return affected;
    }
}
