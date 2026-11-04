using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Trayectos;

/// <summary>
///     Fila de un cargue masivo (tabla de aterrizaje `trayectos_tmp`). Conserva los valores del Excel
///     como texto, sin transformarlos, y agrega estado, observación acumulada e identificador de cargue.
///     La normalización hacia rutas y paradas ocurre en el procesado.
/// </summary>
public sealed class TrayectoTemporal : BaseEntity
{
    private const string SeparadorObservacion = "\n";

    public long IdCargue { get; private set; }

    public string? CodigoRuta { get; private set; }

    public string? IdentificacionConductor { get; private set; }

    public string? PlacaVehiculo { get; private set; }

    public string? Ubicacion { get; private set; }

    public string? OrdenParada { get; private set; }

    public string? Latitud { get; private set; }

    public string? Longitud { get; private set; }

    public string? LoginUsuario { get; private set; }

    public Guid? PersonaId { get; private set; }

    public Guid? VehiculoId { get; private set; }

    public EstadoTrayectoTemporal Estado { get; private set; }

    public string? Observacion { get; private set; }

    private TrayectoTemporal()
    {
    }

    public static TrayectoTemporal Crear(
        long idCargue,
        string? codigoRuta,
        string? identificacionConductor,
        string? placaVehiculo,
        string? ubicacion,
        string? ordenParada,
        string? latitud,
        string? longitud,
        string? loginUsuario,
        DateTime utcNow)
    {
        if (idCargue <= 0)
        {
            throw new DomainValidationException(
                "cargue.identificador",
                ["El identificador de cargue debe ser un número positivo."]);
        }

        var fila = new TrayectoTemporal
        {
            IdCargue = idCargue,
            CodigoRuta = codigoRuta,
            IdentificacionConductor = identificacionConductor,
            PlacaVehiculo = placaVehiculo,
            Ubicacion = ubicacion,
            OrdenParada = ordenParada,
            Latitud = latitud,
            Longitud = longitud,
            LoginUsuario = loginUsuario,
            Estado = EstadoTrayectoTemporal.Cargado,
        };

        fila.Initialize(Guid.NewGuid(), utcNow);
        return fila;
    }

    public void MarcarValidado(Guid personaId, Guid vehiculoId, DateTime utcNow)
    {
        if (Estado == EstadoTrayectoTemporal.Procesado)
        {
            throw new DomainValidationException(
                "cargue.estado",
                ["Un registro ya procesado no se puede volver a validar."]);
        }

        PersonaId = personaId;
        VehiculoId = vehiculoId;
        Estado = EstadoTrayectoTemporal.Validado;
        MarkUpdated(utcNow);
    }

    /// <summary>La observación es acumulativa: cada error nuevo se agrega al final.</summary>
    public void MarcarError(string observacion, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(observacion))
        {
            throw new DomainValidationException(
                "cargue.observacion",
                ["La observación del error es obligatoria."]);
        }

        var texto = observacion.Trim();
        Observacion = string.IsNullOrWhiteSpace(Observacion)
            ? texto
            : Observacion + SeparadorObservacion + texto;
        Estado = EstadoTrayectoTemporal.Error;
        MarkUpdated(utcNow);
    }

    public void MarcarProcesado(DateTime utcNow)
    {
        if (Estado != EstadoTrayectoTemporal.Validado)
        {
            throw new DomainValidationException(
                "cargue.estado",
                ["Solo los registros validados se pueden procesar."]);
        }

        Estado = EstadoTrayectoTemporal.Procesado;
        MarkUpdated(utcNow);
    }

    public bool PuedeProcesarse() => Estado == EstadoTrayectoTemporal.Validado;
}
