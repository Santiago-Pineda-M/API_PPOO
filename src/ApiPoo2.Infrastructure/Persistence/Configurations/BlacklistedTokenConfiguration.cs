using ApiPoo2.Domain.BlacklistedTokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiPoo2.Infrastructure.Persistence.Configurations;

public sealed class BlacklistedTokenConfiguration : IEntityTypeConfiguration<BlacklistedToken>
{
    public void Configure(EntityTypeBuilder<BlacklistedToken> builder)
    {
        builder.ToTable("access_token_blacklist");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.Jti).HasColumnName("jwt_id");
        builder.Property(t => t.PersonaId).HasColumnName("id_persona");
        builder.Property(t => t.ExpiresAtUtc).HasColumnName("expires_at_utc").HasColumnType("timestamptz");
        builder.Property(t => t.RevokedAtUtc).HasColumnName("revoked_at_utc").HasColumnType("timestamptz");
        builder.Property(t => t.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz");
        builder.Property(t => t.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");

        builder.HasIndex(t => t.Jti).IsUnique();
        builder.HasIndex(t => t.ExpiresAtUtc);
    }
}
