using ApiPoo2.Infrastructure.Persistence.Converters;
using ApiPoo2.Domain.Personas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiPoo2.Infrastructure.Persistence.Configurations;

public sealed class PersonaConfiguration : IEntityTypeConfiguration<Persona>
{
    public void Configure(EntityTypeBuilder<Persona> builder)
    {
        builder.ToTable("personas");

        builder.HasCheckConstraint("ck_personas_tipo_identificacion",
            "tipo_identificacion IN ('CC','CE','TI','NIT')");
        builder.HasCheckConstraint("ck_personas_tipo_persona",
            "tipo_persona IN ('ADMINISTRATIVO','CONDUCTOR')");
        builder.HasCheckConstraint("ck_personas_num_identificacion",
            "numero_identificacion ~ '^[0-9]+$'");
        builder.HasCheckConstraint("ck_personas_correo",
            "correo_electronico ~ '^[^@]+@[^@]+\\.[^@]+$'");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz");
        builder.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");

        builder.Property(p => p.TipoIdentificacion)
            .HasColumnName("tipo_identificacion")
            .HasConversion(new TipoIdentificacionConverter())
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(p => p.NumeroIdentificacion)
            .HasColumnName("numero_identificacion")
            .HasConversion(v => v.Value, v => NumeroIdentificacion.From(v))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Nombres)
            .HasColumnName("nombres")
            .HasConversion(v => v.Value, v => Nombres.From(v))
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Apellidos)
            .HasColumnName("apellidos")
            .HasConversion(v => v.Value, v => Apellidos.From(v))
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.CorreoElectronico)
            .HasColumnName("correo_electronico")
            .HasConversion(v => v.Value, v => CorreoElectronico.From(v))
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(p => p.TipoPersona)
            .HasColumnName("tipo_persona")
            .HasConversion(new TipoPersonaConverter())
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(p => p.NumeroIdentificacion).IsUnique();
        builder.HasIndex(p => p.CorreoElectronico).IsUnique();
        builder.HasIndex(p => p.TipoPersona);
    }
}