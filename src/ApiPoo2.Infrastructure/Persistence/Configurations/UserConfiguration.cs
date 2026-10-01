using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiPoo2.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("usuarios");

        builder.HasKey(u => new { u.IdPersona, u.Login });

        builder.Property(u => u.IdPersona).HasColumnName("id_persona");
        builder.Property(u => u.Login)
            .HasColumnName("login")
            .HasConversion(v => v.Value, v => Login.From(v))
            .HasMaxLength(64);

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasConversion(
                v => v.Hash,
                v => PasswordHash.Create(v))
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(u => u.PasswordHashAlgorithm)
            .HasColumnName("password_hash_algorithm")
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(u => u.ApiKey)
            .HasColumnName("api_key")
            .HasConversion(v => v.Value, v => ApiKey.From(v))
            .HasMaxLength(96)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(u => u.AccessFailedCount).HasColumnName("access_failed_count").IsRequired();
        builder.Property(u => u.LockoutEndUtc).HasColumnName("lockout_end_utc").HasColumnType("timestamptz");
        builder.Property(u => u.LastLoginAtUtc).HasColumnName("last_login_at_utc").HasColumnType("timestamptz");
        builder.Property(u => u.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz").IsRequired();
        builder.Property(u => u.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");

        builder.HasIndex(u => u.ApiKey).IsUnique();
        builder.HasIndex(u => u.Login);
        builder.HasIndex(u => new { u.IdPersona, u.ApiKey });

        // La relación con RefreshToken se declara una sola vez en RefreshTokenConfiguration,
        // con la navegación real RefreshTokensVisibles. Declararla acá con el nombre del
        // campo privado duplicaría la relación con una FK fantasma (UserIdPersona).
    }
}
