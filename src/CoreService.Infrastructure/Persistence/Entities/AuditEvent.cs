namespace CoreService.Infrastructure.Persistence.Entities;

public sealed class AuditEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();

    public string EventType { get; set; } = "";       // e.g. "study.grade.updated"
    public Guid? ActorUserId { get; set; }            // nullable for system events
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string EntityType { get; set; } = "";      // e.g. "grade"
    public Guid? EntityId { get; set; }               // nullable

    public string Details { get; set; } = "{}";       // jsonb payload
}
