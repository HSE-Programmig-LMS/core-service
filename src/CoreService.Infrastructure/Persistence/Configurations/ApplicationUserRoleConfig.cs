using CoreService.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreService.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserRoleConfig : IEntityTypeConfiguration<ApplicationUserRole>
{
    public void Configure(EntityTypeBuilder<ApplicationUserRole> e)
    {
        e.ToTable("user_roles");

        e.Property(x => x.UserId).HasColumnName("user_id");
        e.Property(x => x.RoleId).HasColumnName("role_id");
        e.Property(x => x.AssignedAt).HasColumnName("assigned_at");

        e.HasKey(x => new {x.UserId, x.RoleId});

        e.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
        e.HasIndex(x => x.UserId).IsUnique();
    }
}
