using ApiPoo2.Domain.Rutas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiPoo2.Infrastructure.Persistence.Configurations;

public sealed class ParadaConfiguration : IEntityTypeConfiguration<Parada>
{
    public void Configure(EntityTypeBuilder<Parada> builder)
    {
        builder.ToTable("paradas");

        builder.HasCheckConstraint(
            "ck_paradas_orden",
            "orden_parada BETWEEN 0 AND 6");
        builder.HasCheckConstraint(
            "ck_paradas_coordenadas",
            "(latitud IS NULL AND longitud IS NULL) " +
            "OR (latitud IS NOT NULL AND longitud IS NOT NULL)");
        builder.HasCheckConstraint(
            "ck_paradas_latitud",
            "latitud IS NULL OR (latitud BETWEEN -90 AND 90)");
        builder.HasCheckConstraint(
            "ck_paradas_longitud",
            "longitud IS NULL OR (longitud BETWEEN -180 AND 180)");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz").IsRequired();
        builder.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");

        builder.Property(p => p.IdRuta).HasColumnName("id_ruta").IsRequired();

        builder.Property(p => p.Ubicacion)
            .HasColumnName("ubicacion")
            .HasConversion(u => u.Value, v => UbicacionParada.From(v))
            .HasMaxLength(UbicacionParada.MaxLength)
            .IsRequired();

        builder.Property(p => p.OrdenParada).HasColumnName("orden_parada").IsRequired();
        builder.Property(p => p.Latitud).HasColumnName("latitud");
        builder.Property(p => p.Longitud).HasColumnName("longitud");

        builder.HasIndex(p => new { p.IdRuta, p.OrdenParada }).IsUnique();
        builder.HasIndex(p => p.IdRuta);
    }
}
