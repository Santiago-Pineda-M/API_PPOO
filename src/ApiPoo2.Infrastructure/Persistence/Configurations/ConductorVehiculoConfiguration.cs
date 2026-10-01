using ApiPoo2.Infrastructure.Persistence.Converters;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Vehiculos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiPoo2.Infrastructure.Persistence.Configurations;

public sealed class ConductorVehiculoConfiguration : IEntityTypeConfiguration<ConductorVehiculo>
{
    public void Configure(EntityTypeBuilder<ConductorVehiculo> builder)
    {
        builder.ToTable("conductores_vehiculos");

        builder.HasCheckConstraint("ck_conductores_vehiculos_estado",
            "estado IN ('PO','EA','RO')");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz");
        builder.Property(c => c.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");

        builder.Property(c => c.PersonaId).HasColumnName("idpersona").IsRequired();
        builder.Property(c => c.VehiculoId).HasColumnName("idvehiculo").IsRequired();
        builder.Property(c => c.FechaAsociacion).HasColumnName("fecha_asociacion").HasColumnType("timestamptz").IsRequired();

        builder.Property(c => c.Estado)
            .HasColumnName("estado")
            .HasConversion(new EstadoConductorConverter())
            .HasMaxLength(2)
            .IsRequired();

        // Ambas relaciones declaradas una sola vez, con las navegaciones reales de cada
        // extremo. Declararlas también en VehiculoConfiguration duplicaría cada relación
        // con FKs fantasma (PersonaId1, VehiculoId1).
        builder.HasOne(c => c.Persona)
            .WithMany(p => p.ConductoresVehiculosAssociated)
            .HasForeignKey(c => c.PersonaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Vehiculo>()
            .WithMany(v => v.ConductoresAssociated)
            .HasForeignKey(c => c.VehiculoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.PersonaId, c.VehiculoId }).IsUnique();
        builder.HasIndex(c => c.VehiculoId);
    }
}
