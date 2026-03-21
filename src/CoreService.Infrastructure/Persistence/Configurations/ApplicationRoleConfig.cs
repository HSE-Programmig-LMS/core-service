using CoreService.Domain.Security;
using CoreService.Domain.Security;
using CoreService.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreService.Infrastructure.Persistence.Configurations;

public sealed class ApplicationRoleConfig : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> e)
    {
        e.ToTable("roles");

        e.Property(x => x.Id).HasColumnName("role_id");

        // RoleCode enum -> string ("student"/"manager") via mapper
        e.Property(x => x.RoleCode)
            .HasColumnName("role_code")
            .HasConversion(
                v => RoleCodeMapper.ToDb(v),
                v => RoleCodeMapper.FromDb(v));

        e.Property(x => x.RoleName).HasColumnName("role_name");

        e.HasIndex(x => x.RoleCode).IsUnique();

        // Identity still uses Name/NormalizedName internally. We can set them from RoleCode in seeding.
        e.Property(x => x.Name).HasColumnName("name");
        e.Property(x => x.NormalizedName).HasColumnName("normalized_name");
    }
}
