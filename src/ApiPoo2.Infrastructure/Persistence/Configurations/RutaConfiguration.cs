using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Rutas;
using ApiPoo2.Domain.Vehiculos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiPoo2.Infrastructure.Persistence.Configurations;

public sealed class RutaConfiguration : IEntityTypeConfiguration<Ruta>
{
    public void Configure(EntityTypeBuilder<Ruta> builder)
    {
        builder.ToTable("rutas");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz").IsRequired();
        builder.Property(r => r.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");

        builder.Property(r => r.Codigo)
            .HasColumnName("codigo_ruta")
            .HasConversion(c => c.Value, v => CodigoRuta.From(v))
            .HasMaxLength(CodigoRuta.MaxLength)
            .IsRequired();

        builder.Property(r => r.PersonaId).HasColumnName("id_persona").IsRequired();
        builder.Property(r => r.VehiculoId).HasColumnName("id_vehiculo").IsRequired();

        builder.Property(r => r.LoginUsuario)
            .HasColumnName("login_usuario")
            .HasConversion(l => l.Value, v => LoginUsuario.From(v))
            .HasMaxLength(LoginUsuario.MaxLength)
            .IsRequired();

        builder.HasOne(r => r.Persona)
            .WithMany()
            .HasForeignKey(r => r.PersonaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Vehiculo)
            .WithMany()
            .HasForeignKey(r => r.VehiculoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Paradas)
            .WithOne()
            .HasForeignKey(p => p.IdRuta)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(r => r.Paradas)
            .HasField("_paradas")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(r => r.Codigo).IsUnique();
        builder.HasIndex(r => r.PersonaId);
        builder.HasIndex(r => r.VehiculoId);
        builder.HasIndex(r => r.LoginUsuario);
    }
}
