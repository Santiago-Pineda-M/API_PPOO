using ApiPoo2.Infrastructure.Persistence.Converters;
using ApiPoo2.Domain.Documentos;
using ApiPoo2.Domain.Vehiculos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiPoo2.Infrastructure.Persistence.Configurations;

public sealed class VehiculoDocumentoConfiguration : IEntityTypeConfiguration<VehiculoDocumento>
{
    public void Configure(EntityTypeBuilder<VehiculoDocumento> builder)
    {
        builder.ToTable("vehiculos_documentos");

        builder.HasCheckConstraint("ck_vehiculos_documentos_estado",
            "estado IN ('HABILITADO','VENCIDO','EN_VERIFICACION')");
        builder.HasCheckConstraint("ck_vehiculos_documentos_contenido",
            "octet_length(contenido) > 0");

        builder.HasKey(d => new { d.VehiculoId, d.DocumentoId });

        builder.Property(d => d.VehiculoId).HasColumnName("idvehiculo");
        builder.Property(d => d.DocumentoId).HasColumnName("iddocumento");

        builder.Property(d => d.NombreArchivo)
            .HasColumnName("nombre_archivo")
            .HasConversion(n => n.Value, n => NombreArchivo.From(n))
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(d => d.Contenido)
            .HasColumnName("contenido")
            .HasConversion(
                c => c.ToArray(),
                b => ContenidoDocumento.From(b, "documento.pdf"))
            .HasColumnType("bytea")
            .IsRequired();

        builder.Property(d => d.FechaExpedicion)
            .HasColumnName("fecha_expedicion")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(d => d.FechaVencimiento)
            .HasColumnName("fecha_vencimiento")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(d => d.Estado)
            .HasColumnName("estado")
            .HasConversion(new EstadoDocumentoConverter())
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz").IsRequired();
        builder.Property(d => d.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");

        // Relación declarada una sola vez: la navegación real del principal es
        // Vehiculo.DocumentosAssociated. Declararla también en VehiculoConfiguration
        // duplicaría la relación con una FK fantasma (VehiculoId1).
        builder.HasOne<Vehiculo>()
            .WithMany(v => v.DocumentosAssociated)
            .HasForeignKey(d => d.VehiculoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Documento>()
            .WithMany()
            .HasForeignKey(d => d.DocumentoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
