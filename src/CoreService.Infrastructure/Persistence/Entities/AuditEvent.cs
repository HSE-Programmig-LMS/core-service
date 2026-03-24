namespace CoreService.Infrastructure.Persistence.Entities;

public sealed class AuditEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();

    public string EventType { get; set; } = "";
    public Guid? ActorUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string EntityType { get; set; } = "";
    public Guid? EntityId { get; set; }

    public string Details { get; set; } = "{}";
}
