namespace CoreService.Application.Abstractions.Audit;

public interface IAuditWriter
{
    Task WriteAsync(AuditWriteEntry entry, CancellationToken ct = default);
}

/// <summary>
/// Минимальная запись аудита
/// DetailsJson — строка JSON.
/// </summary>
public sealed record AuditWriteEntry(
    string EventType,
    Guid? ActorUserId,
    string EntityType,
    Guid? EntityId,
    string DetailsJson,
    Guid? EventId = null,
    DateTimeOffset? CreatedAtUtc = null);
