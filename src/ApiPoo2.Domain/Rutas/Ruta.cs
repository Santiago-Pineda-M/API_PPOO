using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Vehiculos;

namespace ApiPoo2.Domain.Rutas;

/// <summary>
///     Ruta realizada por un conductor con un vehículo. Cabecera normalizada: el código, el
///     conductor, el vehículo y el login viven una sola vez; las paradas van en la colección.
/// </summary>
public sealed class Ruta : BaseEntity
{
    private readonly List<Parada> _paradas = [];

    public CodigoRuta Codigo { get; private set; } = null!;

    public Guid PersonaId { get; private set; }

    public Guid VehiculoId { get; private set; }

    public LoginUsuario LoginUsuario { get; private set; } = null!;

    public Persona Persona { get; private set; } = null!;

    public Vehiculo Vehiculo { get; private set; } = null!;

    public IReadOnlyList<Parada> Paradas => _paradas;

    private Ruta()
    {
    }

    /// <summary>
    ///     Crea la ruta con su parada inicial (orden 0) y su parada final (orden 1). El enunciado
    ///     exige como mínimo una parada inicial y una final, así que la ruta nunca nace incompleta:
    ///     las paradas intermedias se agregan después y la final pasa a ser la de mayor orden.
    /// </summary>
    public static Ruta Registrar(
        string codigoRuta,
        Guid personaId,
        Guid vehiculoId,
        string loginUsuario,
        string ubicacionInicial,
        double? latitud,
        double? longitud,
        string ubicacionFinal,
        double? latitudFinal,
        double? longitudFinal,
        DateTime utcNow)
    {
        if (personaId == Guid.Empty)
        {
            throw new DomainValidationException("ruta.conductor", ["La ruta debe tener un conductor."]);
        }

        if (vehiculoId == Guid.Empty)
        {
            throw new DomainValidationException("ruta.vehiculo", ["La ruta debe tener un vehículo."]);
        }

        var ruta = new Ruta
        {
            Codigo = CodigoRuta.From(codigoRuta),
            PersonaId = personaId,
            VehiculoId = vehiculoId,
            LoginUsuario = LoginUsuario.From(loginUsuario),
        };

        ruta.Initialize(Guid.NewGuid(), utcNow);
        ruta.AgregarParadaInterna(ubicacionInicial, ReglasParada.OrdenInicial, latitud, longitud, utcNow);
        ruta.AgregarParadaInterna(
            ubicacionFinal,
            ReglasParada.OrdenFinalMinimo,
            latitudFinal,
            longitudFinal,
            utcNow);

        return ruta;
    }

    public Parada AgregarParada(
        string ubicacion,
        int ordenParada,
        double? latitud,
        double? longitud,
        DateTime utcNow)
    {
        ReglasParada.EnsureValid(ordenParada);

        if (_paradas.Count >= ReglasParada.MaximoParadasPorRuta)
        {
            throw new DomainValidationException(
                "parada.maximo",
                [$"Una ruta admite como máximo {ReglasParada.MaximoParadasPorRuta} paradas."]);
        }

        if (_paradas.Any(p => p.OrdenParada == ordenParada))
        {
            throw new DomainValidationException(
                "parada.orden.duplicado",
                [$"La ruta ya tiene una parada en el orden {ordenParada}."]);
        }

        var parada = AgregarParadaInterna(ubicacion, ordenParada, latitud, longitud, utcNow);
        MarkUpdated(utcNow);

        return parada;
    }

    public void ActualizarParada(
        Guid paradaId,
        string ubicacion,
        int ordenParada,
        double? latitud,
        double? longitud,
        DateTime utcNow)
    {
        var parada = BuscarParada(paradaId)
            ?? throw new DomainValidationException(
                "parada.not_found",
                ["La ruta no tiene esa parada."]);

        if (_paradas.Any(p => p.Id != paradaId && p.OrdenParada == ordenParada))
        {
            throw new DomainValidationException(
                "parada.orden.duplicado",
                [$"La ruta ya tiene una parada en el orden {ordenParada}."]);
        }

        parada.Actualizar(ubicacion, ordenParada, latitud, longitud, utcNow);
        MarkUpdated(utcNow);
    }

    /// <summary>La parada inicial identifica la ruta: para eliminarla hay que eliminar la ruta.</summary>
    public void EliminarParada(Guid paradaId, DateTime utcNow)
    {
        var parada = BuscarParada(paradaId)
            ?? throw new DomainValidationException(
                "parada.not_found",
                ["La ruta no tiene esa parada."]);

        if (parada.EsInicial())
        {
            throw new DomainValidationException(
                "parada.inicial.requerida",
                ["No se puede eliminar la parada inicial; eliminá la ruta completa."]);
        }

        _paradas.Remove(parada);
        MarkUpdated(utcNow);
    }

    public void AsignarCoordenadas(Guid paradaId, double latitud, double longitud, DateTime utcNow)
    {
        var parada = BuscarParada(paradaId)
            ?? throw new DomainValidationException(
                "parada.not_found",
                ["La ruta no tiene esa parada."]);

        parada.AsignarCoordenadas(latitud, longitud, utcNow);
        MarkUpdated(utcNow);
    }

    public void Reasignar(Guid personaId, Guid vehiculoId, DateTime utcNow)
    {
        if (personaId == Guid.Empty)
        {
            throw new DomainValidationException("ruta.conductor", ["La ruta debe tener un conductor."]);
        }

        if (vehiculoId == Guid.Empty)
        {
            throw new DomainValidationException("ruta.vehiculo", ["La ruta debe tener un vehículo."]);
        }

        if (PersonaId == personaId && VehiculoId == vehiculoId)
        {
            return;
        }

        PersonaId = personaId;
        VehiculoId = vehiculoId;
        MarkUpdated(utcNow);
    }

    public Parada? BuscarParada(Guid paradaId) => _paradas.FirstOrDefault(p => p.Id == paradaId);

    /// <summary>Una ruta está completa cuando tiene parada inicial y al menos una final distinta.</summary>
    public bool EstaCompleta() => _paradas.Count >= 2 && _paradas.Any(p => p.EsInicial());

    public int CantidadParadas() => _paradas.Count;

    private Parada AgregarParadaInterna(
        string ubicacion,
        int ordenParada,
        double? latitud,
        double? longitud,
        DateTime utcNow)
    {
        var parada = Parada.Crear(Id, ubicacion, ordenParada, latitud, longitud, utcNow);
        _paradas.Add(parada);

        return parada;
    }
}
