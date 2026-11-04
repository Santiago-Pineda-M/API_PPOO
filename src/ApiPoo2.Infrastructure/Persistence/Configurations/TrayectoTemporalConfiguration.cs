using ApiPoo2.Domain.Trayectos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiPoo2.Infrastructure.Persistence.Configurations;

/// <summary>
///     Tabla de aterrizaje del cargue masivo. Se mantiene denormalizada a propósito: conserva los
///     valores del Excel tal como llegaron para poder auditar y reprocesar el cargue.
/// </summary>
public sealed class TrayectoTemporalConfiguration : IEntityTypeConfiguration<TrayectoTemporal>
{
    public void Configure(EntityTypeBuilder<TrayectoTemporal> builder)
    {
        builder.ToTable("trayectos_tmp");

        builder.HasCheckConstraint(
            "ck_trayectos_tmp_estado",
            "estado IN ('CARGADO','VALIDADO','PROCESADO','ERROR')");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz").IsRequired();
        builder.Property(t => t.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");

        builder.Property(t => t.IdCargue).HasColumnName("id_cargue").IsRequired();

        builder.Property(t => t.CodigoRuta).HasColumnName("codigo_ruta").HasMaxLength(32);
        builder.Property(t => t.IdentificacionConductor)
            .HasColumnName("identificacion_conductor")
            .HasMaxLength(20);
        builder.Property(t => t.PlacaVehiculo).HasColumnName("placa_vehiculo").HasMaxLength(10);
        builder.Property(t => t.Ubicacion).HasColumnName("ubicacion").HasMaxLength(200);
        builder.Property(t => t.OrdenParada).HasColumnName("orden_parada").HasMaxLength(10);
        builder.Property(t => t.Latitud).HasColumnName("latitud").HasMaxLength(30);
        builder.Property(t => t.Longitud).HasColumnName("longitud").HasMaxLength(30);
        builder.Property(t => t.LoginUsuario).HasColumnName("login_usuario").HasMaxLength(64);

        builder.Property(t => t.PersonaId).HasColumnName("id_persona");
        builder.Property(t => t.VehiculoId).HasColumnName("id_vehiculo");

        builder.Property(t => t.Estado)
            .HasColumnName("estado")
            .HasConversion(new Converters.EstadoTrayectoTemporalConverter())
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(t => t.Observacion).HasColumnName("observacion");

        builder.HasOne<ApiPoo2.Domain.Personas.Persona>()
            .WithMany()
            .HasForeignKey(t => t.PersonaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<ApiPoo2.Domain.Vehiculos.Vehiculo>()
            .WithMany()
            .HasForeignKey(t => t.VehiculoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => new { t.IdCargue, t.Estado });
    }
}
