using ApiPoo2.Domain.Vehiculos;
using ApiPoo2.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiPoo2.Infrastructure.Persistence.Configurations;

public sealed class VehiculoConfiguration : IEntityTypeConfiguration<Vehiculo>
{
    public void Configure(EntityTypeBuilder<Vehiculo> builder)
    {
        builder.ToTable("vehiculos");

        builder.HasCheckConstraint("ck_vehiculos_tipo_vehiculo",
            "tipo_vehiculo IN ('AUTOMOVIL','MOTOCIClETA')");
        builder.HasCheckConstraint("ck_vehiculos_tipo_servicio",
            "tipo_servicio IN ('PUBLICO','PRIVADO')");
        builder.HasCheckConstraint("ck_vehiculos_tipo_combustible",
            "tipo_combustible IN ('GASOLINA','GAS','DISEL')");
        builder.HasCheckConstraint("ck_vehiculos_capacidad_pasajeros",
            "capacidad_pasajeros >= 0");
        builder.HasCheckConstraint("ck_vehiculos_modelo",
            "modelo > 0");

        // El formato de la placa depende del tipo: automóvil 3 letras + 3 números,
        // motocicleta 3 letras + 2 números + 1 letra.
        builder.HasCheckConstraint("ck_vehiculos_placa",
            "(tipo_vehiculo = 'AUTOMOVIL' AND placa ~ '^[A-Z]{3}[0-9]{3}$') " +
            "OR (tipo_vehiculo = 'MOTOCIClETA' AND placa ~ '^[A-Z]{3}[0-9]{2}[A-Z]$')");

        builder.HasCheckConstraint("ck_vehiculos_color",
            "color ~ '^#[0-9A-Fa-f]{6}$'");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id");
        builder.Property(v => v.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz");
        builder.Property(v => v.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");

        builder.Property(v => v.Placa)
            .HasColumnName("placa")
            .HasConversion(p => p.Value, v => Placa.From(v, TipoVehiculo.Automovil))
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(v => v.TipoVehiculo)
            .HasColumnName("tipo_vehiculo")
            .HasConversion(new TipoVehiculoConverter())
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(v => v.TipoServicio)
            .HasColumnName("tipo_servicio")
            .HasConversion(new TipoServicioConverter())
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(v => v.Combustible)
            .HasColumnName("tipo_combustible")
            .HasConversion(new TipoCombustibleConverter())
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(v => v.CapacidadPasajeros).HasColumnName("capacidad_pasajeros").IsRequired();

        builder.Property(v => v.Color)
            .HasColumnName("color")
            .HasConversion(c => c.Value, c => Color.From(c))
            .HasMaxLength(7)
            .IsRequired();

        builder.Property(v => v.Modelo).HasColumnName("modelo").IsRequired();

        builder.Property(v => v.Marca)
            .HasColumnName("marca")
            .HasConversion(m => m.Value, m => Marca.From(m))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(v => v.Linea)
            .HasColumnName("linea")
            .HasConversion(l => l.Value, l => Linea.From(l))
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(v => v.Placa).IsUnique();
        builder.HasIndex(v => v.TipoVehiculo);
    }
}
