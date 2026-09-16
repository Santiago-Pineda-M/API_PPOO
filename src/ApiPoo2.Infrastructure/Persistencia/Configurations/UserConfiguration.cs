using ApiPoo2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiPoo2.Infrastructure.Persistencia.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz");
        builder.Property(u => u.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .HasConversion(new EmailConverter())
            .IsRequired();

        builder.OwnsOne(u => u.PasswordHash, password =>
        {
            password.Property(p => p.Algorithm).HasColumnName("password_hash_algorithm").HasMaxLength(16).IsRequired();
            password.Property(p => p.Hash).HasColumnName("password_hash").HasMaxLength(256).IsRequired();
        });

        builder.Property(u => u.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(u => u.IsActive).HasColumnName("is_active");
        builder.Property(u => u.AccessFailedCount).HasColumnName("access_failed_count");
        builder.Property(u => u.LockoutEndUtc).HasColumnName("lockout_end_utc");
        builder.Property(u => u.LastLoginAtUtc).HasColumnName("last_login_at_utc").HasColumnType("timestamptz");

        builder.HasIndex(u => u.Email).IsUnique();

        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(t => t.UserId)
            .HasPrincipalKey(u => u.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}