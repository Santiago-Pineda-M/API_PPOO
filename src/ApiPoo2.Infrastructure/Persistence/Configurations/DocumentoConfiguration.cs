using ApiPoo2.Domain.Documentos;
using ApiPoo2.Domain.Vehiculos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiPoo2.Infrastructure.Persistence.Configurations;

public sealed class DocumentoConfiguration : IEntityTypeConfiguration<Documento>
{
    public void Configure(EntityTypeBuilder<Documento> builder)
    {
        builder.ToTable("documentos");

        builder.HasCheckConstraint("ck_documentos_tipos_vehiculo_aplicables",
            "tipos_vehiculo_aplicables IN ('A','M','AM')");
        builder.HasCheckConstraint("ck_documentos_codigo_obligatoriedad",
            "codigo_obligatoriedad IN ('RA','RM','RR')");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");
        builder.Property(d => d.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz");
        builder.Property(d => d.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");

        builder.Property(d => d.Codigo)
            .HasColumnName("codigo")
            .HasConversion(c => c.Value, c => DocumentoCodigo.From(c))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(d => d.Nombre)
            .HasColumnName("nombre")
            .HasConversion(n => n.Value, n => DocumentoNombre.From(n))
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.TiposVehiculoAplicables)
            .HasColumnName("tipos_vehiculo_aplicables")
            .HasConversion(t => t.Value, t => TiposVehiculoAplicables.From(t))
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(d => d.CodigoObligatoriedad)
            .HasColumnName("codigo_obligatoriedad")
            .HasConversion(o => o.Value, o => CodigoObligatoriedad.From(o))
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(d => d.Descripcion)
            .HasColumnName("descripcion")
            .HasConversion(x => x.Value, x => DocumentoDescripcion.From(x))
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(d => d.Codigo).IsUnique();
    }
}
