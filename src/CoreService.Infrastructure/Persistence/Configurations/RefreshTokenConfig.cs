using CoreService.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreService.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> e)
    {
        e.ToTable("tokens");

        e.HasKey(x => x.TokenId);

        e.Property(x => x.TokenId).HasColumnName("token_id");
        e.Property(x => x.UserId).HasColumnName("user_id");
        e.Property(x => x.TokenHash).HasColumnName("token_hash");
        e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        e.Property(x => x.RevokedAt).HasColumnName("revoked_at");

        e.HasIndex(x => x.UserId);
        e.HasIndex(x => x.TokenHash).IsUnique();
    }
}
