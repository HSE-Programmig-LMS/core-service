using CoreService.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreService.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserRoleConfig : IEntityTypeConfiguration<ApplicationUserRole>
{
    public void Configure(EntityTypeBuilder<ApplicationUserRole> e)
    {
        e.ToTable("user_roles");

        e.Property(x => x.UserRoleId).HasColumnName("user_role_id");
        e.Property(x => x.UserId).HasColumnName("user_id");
        e.Property(x => x.RoleId).HasColumnName("role_id");
        e.Property(x => x.AssignedAt).HasColumnName("assigned_at");

        // Primary key = user_role_id (как у вас в модели)
        e.HasKey(x => x.UserRoleId);

        // Identity при операциях часто использует пару (UserId, RoleId)
        // Оставим уникальность по паре для корректности, и главное — UNIQUE(UserId) => 1 роль на пользователя.
        e.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
        e.HasIndex(x => x.UserId).IsUnique();
    }
}
