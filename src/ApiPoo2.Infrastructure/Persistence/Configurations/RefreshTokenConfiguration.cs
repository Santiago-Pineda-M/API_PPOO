using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.RefreshTokens;
using ApiPoo2.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiPoo2.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.PersonaId).HasColumnName("id_persona");
        builder.Property(t => t.Login)
            .HasColumnName("login")
            .HasConversion(v => v.Value, v => Login.From(v))
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(t => t.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
        builder.Property(t => t.ExpiresAtUtc).HasColumnName("expires_at_utc").HasColumnType("timestamptz");
        builder.Property(t => t.IsUsed).HasColumnName("is_used");
        builder.Property(t => t.UsedAtUtc).HasColumnName("used_at_utc").HasColumnType("timestamptz");
        builder.Property(t => t.IsRevoked).HasColumnName("is_revoked");
        builder.Property(t => t.RevokedAtUtc).HasColumnName("revoked_at_utc").HasColumnType("timestamptz");
        builder.Property(t => t.RevokedReason).HasColumnName("revoked_reason").HasConversion<string>().HasMaxLength(32);
        builder.Property(t => t.ReplacedByTokenId).HasColumnName("replaced_by_token_id");
        builder.Property(t => t.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz");
        builder.Property(t => t.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");
        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => new { t.PersonaId, t.Login });
        builder.HasIndex(t => t.ExpiresAtUtc);

        // Relación declarada una sola vez, con la navegación real del principal
        // (User.RefreshTokensVisibles). Declararla también en UserConfiguration con el
        // nombre del campo privado duplicaría la relación con una FK fantasma.
        builder.HasOne<User>()
            .WithMany(u => u.RefreshTokensVisibles)
            .HasForeignKey(t => new { t.PersonaId, t.Login })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
