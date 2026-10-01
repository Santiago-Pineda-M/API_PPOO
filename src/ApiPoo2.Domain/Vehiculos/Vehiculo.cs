using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.TiposDocumento;
using ApiPoo2.Domain.Personas;

namespace ApiPoo2.Domain.Vehiculos;

public sealed class Vehiculo : BaseEntity
{
    public Placa Placa { get; private set; } = null!;

    public TipoVehiculo TipoVehiculo { get; private set; }

    public TipoServicio TipoServicio { get; private set; }

    public TipoCombustible Combustible { get; private set; }

    public int CapacidadPasajeros { get; private set; }

    public Color Color { get; private set; } = null!;

    public int Modelo { get; private set; }

    public Marca Marca { get; private set; } = null!;

    public Linea Linea { get; private set; } = null!;

    private List<DocumentoVehiculo> Documentos { get; set; } = [];

    private List<ConductorVehiculo> Conductores { get; set; } = [];

    private Vehiculo()
    {
    }

    public static Vehiculo Register(
        string placa,
        TipoVehiculo tipoVehiculo,
        TipoServicio tipoServicio,
        TipoCombustible combustible,
        int capacidadPasajeros,
        string color,
        int modelo,
        string marca,
        string linea,
        DateTime utcNow)
    {
        if (capacidadPasajeros < 0)
        {
            throw new DomainValidationException(
                "vehiculo.capacidad",
                ["La capacidad de pasajeros no puede ser negativa."]);
        }

        if (modelo <= 0)
        {
            throw new DomainValidationException(
                "vehiculo.modelo",
                ["El modelo debe ser un número entero positivo."]);
        }

        var vehiculo = new Vehiculo
        {
            Placa = Placa.From(placa, tipoVehiculo),
            TipoVehiculo = tipoVehiculo,
            TipoServicio = tipoServicio,
            Combustible = combustible,
            CapacidadPasajeros = capacidadPasajeros,
            Color = Color.From(color),
            Modelo = modelo,
            Marca = Marca.From(marca),
            Linea = Linea.From(linea),
        };

        vehiculo.Initialize(Guid.NewGuid(), utcNow);
        return vehiculo;
    }

    /// <summary>
    ///     El enunciado exige registrar vehículo y documento en una sola operación: el vehículo
    ///     nunca queda sin documento y el documento nace En Verificación.
    /// </summary>
    public DocumentoVehiculo AdjuntarDocumento(
        TipoDocumento documento,
        byte[] contenido,
        string nombreArchivo,
        DateTime fechaExpedicion,
        DateTime fechaVencimiento,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(documento);

        if (!documento.AplicaA(TipoVehiculo))
        {
            throw new DomainValidationException(
                "vehiculo.documento.incompatible",
                [$"El documento '{documento.Nombre.Value}' no aplica a vehículos de tipo {TipoVehiculo}."]);
        }

        if (Documentos.Any(d => d.TipoDocumentoId == documento.Id))
        {
            throw new DomainValidationException(
                "vehiculo.documento.duplicado",
                ["El vehículo ya tiene asociado ese tipo de documento."]);
        }

        var vehiculoDocumento = DocumentoVehiculo.Crear(
            Id,
            documento.Id,
            contenido,
            nombreArchivo,
            fechaExpedicion,
            fechaVencimiento,
            utcNow);

        Documentos.Add(vehiculoDocumento);
        MarkUpdated(utcNow);

        return vehiculoDocumento;
    }

    public ConductorVehiculo AsociarConductor(Guid idPersona, DateTime utcNow, EstadoConductor estado = EstadoConductor.Ea)
    {
        if (Conductores.Any(c => c.PersonaId == idPersona))
        {
            throw new DomainValidationException(
                "vehiculo.conductor.duplicado",
                ["El conductor ya está asociado a este vehículo."]);
        }

        var conductor = ConductorVehiculo.Crear(idPersona, Id, utcNow, estado);

        Conductores.Add(conductor);
        MarkUpdated(utcNow);

        return conductor;
    }

    public bool TieneDocumentos() => Documentos.Count > 0;

    /// <summary>
    ///     Actualiza los datos del vehículo. Si cambia el tipo, todos los documentos ya asociados
    ///     deben seguir siendo aplicables al nuevo tipo.
    /// </summary>
    public void Actualizar(
        string placa,
        TipoVehiculo tipoVehiculo,
        TipoServicio tipoServicio,
        TipoCombustible combustible,
        int capacidadPasajeros,
        string color,
        int modelo,
        string marca,
        string linea,
        DateTime utcNow)
    {
        if (capacidadPasajeros < 0)
        {
            throw new DomainValidationException(
                "vehiculo.capacidad",
                ["La capacidad de pasajeros no puede ser negativa."]);
        }

        if (modelo <= 0)
        {
            throw new DomainValidationException(
                "vehiculo.modelo",
                ["El modelo debe ser un número entero positivo."]);
        }

        if (tipoVehiculo != TipoVehiculo && TieneDocumentos())
        {
            throw new DomainValidationException(
                "vehiculo.tipo.cambio_con_documentos",
                ["No se puede cambiar el tipo de un vehículo que ya tiene documentos asociados."]);
        }

        var placaValida = Placa.From(placa, tipoVehiculo);

        Placa = placaValida;
        TipoVehiculo = tipoVehiculo;
        TipoServicio = tipoServicio;
        Combustible = combustible;
        CapacidadPasajeros = capacidadPasajeros;
        Color = Color.From(color);
        Modelo = modelo;
        Marca = Marca.From(marca);
        Linea = Linea.From(linea);
        MarkUpdated(utcNow);
    }

    /// <summary>
    ///     Carga un documento nuevo o reemplaza el archivo de uno ya asociado, como pide el enunciado.
    /// </summary>
    public DocumentoVehiculo GuardarDocumento(
        TipoDocumento documento,
        byte[] contenido,
        string nombreArchivo,
        DateTime fechaExpedicion,
        DateTime fechaVencimiento,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(documento);

        var existente = Documentos.FirstOrDefault(d => d.TipoDocumentoId == documento.Id);
        if (existente is not null)
        {
            if (!documento.AplicaA(TipoVehiculo))
            {
                throw new DomainValidationException(
                    "vehiculo.documento.incompatible",
                    [$"El documento '{documento.Nombre.Value}' no aplica a vehículos de tipo {TipoVehiculo}."]);
            }

            existente.ReemplazarContenido(contenido, nombreArchivo, fechaExpedicion, fechaVencimiento, utcNow);
            MarkUpdated(utcNow);
            return existente;
        }

        return AdjuntarDocumento(documento, contenido, nombreArchivo, fechaExpedicion, fechaVencimiento, utcNow);
    }

    /// <summary>Navegación de lectura para materialización de EF; no permite mutar la colección.</summary>
    public IReadOnlyList<DocumentoVehiculo> DocumentosAssociated => Documentos;

    /// <summary>Navegación de lectura para materialización de EF; no permite mutar la colección.</summary>
    public IReadOnlyList<ConductorVehiculo> ConductoresAssociated => Conductores;

    /// <summary>Vista de solo lectura: la colección sigue siendo privada y no se puede mutar desde afuera.</summary>
    public IReadOnlyList<DocumentoVehiculo> GetTiposDocumento() => Documentos;

    /// <summary>Vista de solo lectura de los conductores asociados.</summary>
    public IReadOnlyList<ConductorVehiculo> GetConductores() => Conductores;
}
