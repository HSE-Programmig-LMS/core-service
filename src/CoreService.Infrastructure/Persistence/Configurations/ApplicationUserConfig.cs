using CoreService.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreService.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfig : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> e)
    {
        e.ToTable("users");

        e.Property(x => x.Id).HasColumnName("user_id");
        e.Property(x => x.Email).HasColumnName("email");
        e.Property(x => x.NormalizedEmail).HasColumnName("normalized_email");
        e.Property(x => x.UserName).HasColumnName("username");
        e.Property(x => x.NormalizedUserName).HasColumnName("normalized_username");

        e.Property(x => x.PasswordHash).HasColumnName("password_hash");

        e.Property(x => x.FirstName).HasColumnName("first_name");
        e.Property(x => x.LastName).HasColumnName("last_name");

        e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.Property(x => x.LastLoginAt).HasColumnName("last_login_at");

        e.HasIndex(x => x.NormalizedEmail).IsUnique();
    }
}
