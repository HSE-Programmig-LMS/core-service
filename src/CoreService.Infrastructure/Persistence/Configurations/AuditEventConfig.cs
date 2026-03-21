using CoreService.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreService.Infrastructure.Persistence.Configurations;

public sealed class AuditEventConfig : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> e)
    {
        e.ToTable("audit_events");

        e.HasKey(x => x.EventId);

        e.Property(x => x.EventId).HasColumnName("event_id");
        e.Property(x => x.EventType).HasColumnName("event_type");
        e.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.Property(x => x.EntityType).HasColumnName("entity_type");
        e.Property(x => x.EntityId).HasColumnName("entity_id");

        e.Property(x => x.Details)
            .HasColumnName("details")
            .HasColumnType("jsonb");

        e.HasIndex(x => x.CreatedAt);
        e.HasIndex(x => x.EventId).IsUnique(); // дедупликация на ingest
    }
}
