using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.TiposDocumento;

/// <summary>
///     Relación entre vehículo y documento paramétrico. Acá vive el archivo: un mismo tipo de
///     documento puede estar asociado a muchos vehículos sin compartir el contenido.
/// </summary>
public sealed class DocumentoVehiculo
{
    public Guid VehiculoId { get; private set; }

    public Guid TipoDocumentoId { get; private set; }

    public NombreArchivo NombreArchivo { get; private set; } = null!;

    public ContenidoDocumento Contenido { get; private set; } = null!;

    public DateTime FechaExpedicion { get; private set; }

    public DateTime FechaVencimiento { get; private set; }

    public EstadoDocumento Estado { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    private DocumentoVehiculo()
    {
    }

    public static DocumentoVehiculo Crear(
        Guid vehiculoId,
        Guid tipoDocumentoId,
        byte[] contenido,
        string nombreArchivo,
        DateTime fechaExpedicion,
        DateTime fechaVencimiento,
        DateTime utcNow)
    {
        if (vehiculoId == Guid.Empty)
        {
            throw new DomainValidationException(
                "vehiculo_documento.vehiculo",
                ["El vehículo es obligatorio."]);
        }

        if (tipoDocumentoId == Guid.Empty)
        {
            throw new DomainValidationException(
                "vehiculo_documento.documento",
                ["El documento es obligatorio."]);
        }

        if (fechaVencimiento <= fechaExpedicion)
        {
            throw new DomainValidationException(
                "vehiculo_documento.vencimiento",
                ["La fecha de vencimiento debe ser posterior a la fecha de expedición."]);
        }

        if (fechaExpedicion > utcNow)
        {
            throw new DomainValidationException(
                "vehiculo_documento.expedicion",
                ["La fecha de expedición no puede ser futura."]);
        }

        var relacion = new DocumentoVehiculo
        {
            VehiculoId = vehiculoId,
            TipoDocumentoId = tipoDocumentoId,
            NombreArchivo = NombreArchivo.From(nombreArchivo),
            Contenido = ContenidoDocumento.From(contenido, nombreArchivo),
            FechaExpedicion = fechaExpedicion,
            FechaVencimiento = fechaVencimiento,
            Estado = EstadoDocumento.EnVerificacion,
            CreatedAtUtc = utcNow,
        };

        return relacion;
    }

    /// <summary>
    ///     Reemplaza el archivo de un documento ya asociado. El enunciado pide cargue y/o
    ///     actualización en el mismo servicio.
    /// </summary>
    public void ReemplazarContenido(
        byte[] contenido,
        string nombreArchivo,
        DateTime fechaExpedicion,
        DateTime fechaVencimiento,
        DateTime utcNow)
    {
        if (fechaVencimiento <= fechaExpedicion)
        {
            throw new DomainValidationException(
                "vehiculo_documento.vencimiento",
                ["La fecha de vencimiento debe ser posterior a la fecha de expedición."]);
        }

        if (fechaExpedicion > utcNow)
        {
            throw new DomainValidationException(
                "vehiculo_documento.expedicion",
                ["La fecha de expedición no puede ser futura."]);
        }

        Contenido = ContenidoDocumento.From(contenido, nombreArchivo, Contenido.ContentType);
        NombreArchivo = NombreArchivo.From(nombreArchivo);
        FechaExpedicion = fechaExpedicion;
        FechaVencimiento = fechaVencimiento;
        Estado = EstadoDocumento.EnVerificacion;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    ///     Sincroniza el estado con la fecha de vencimiento. Los documentos vencidos no se guardan
    ///     como Vencido: se derivan al consultar, para que un documento no quede desactualizado.
    /// </summary>
    public EstadoDocumento EstadoActual(DateTime utcNow)
        => utcNow >= FechaVencimiento ? EstadoDocumento.Vencido : Estado;

    /// <summary>
    ///     Cambia el estado almacenado. <c>Vencido</c> no se asigna manualmente porque siempre se
    ///     deriva de la fecha de vencimiento al consultar.
    /// </summary>
    public void CambiarEstado(EstadoDocumento estado, DateTime utcNow)
    {
        if (estado == EstadoDocumento.Vencido)
        {
            throw new DomainValidationException(
                "vehiculo_documento.estado",
                ["El estado Vencido se deriva de la fecha de vencimiento y no se asigna manualmente."]);
        }

        if (Estado == estado)
        {
            return;
        }

        Estado = estado;
        UpdatedAtUtc = utcNow;
    }
}
