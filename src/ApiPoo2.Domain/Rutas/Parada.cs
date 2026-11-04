using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Rutas;

/// <summary>
///     Parada de una ruta. La ruta es el agregado y la parada su entidad hija: solo se crea y muta
///     a través de <see cref="Ruta" />.
/// </summary>
public sealed class Parada : BaseEntity
{
    public Guid IdRuta { get; private set; }

    public UbicacionParada Ubicacion { get; private set; } = null!;

    public int OrdenParada { get; private set; }

    public double? Latitud { get; private set; }

    public double? Longitud { get; private set; }

    private Parada()
    {
    }

    public static Parada Crear(
        Guid idRuta,
        string ubicacion,
        int ordenParada,
        double? latitud,
        double? longitud,
        DateTime utcNow)
    {
        if (idRuta == Guid.Empty)
        {
            throw new DomainValidationException(
                "parada.ruta",
                ["La parada debe pertenecer a una ruta."]);
        }

        ReglasParada.EnsureValid(ordenParada);
        EnsureCoordenadasValidas(latitud, longitud);

        var parada = new Parada
        {
            IdRuta = idRuta,
            Ubicacion = UbicacionParada.From(ubicacion),
            OrdenParada = ordenParada,
            Latitud = latitud,
            Longitud = longitud,
        };

        parada.Initialize(Guid.NewGuid(), utcNow);
        return parada;
    }

    public void Actualizar(
        string ubicacion,
        int ordenParada,
        double? latitud,
        double? longitud,
        DateTime utcNow)
    {
        ReglasParada.EnsureValid(ordenParada);
        EnsureCoordenadasValidas(latitud, longitud);

        Ubicacion = UbicacionParada.From(ubicacion);
        OrdenParada = ordenParada;
        Latitud = latitud;
        Longitud = longitud;
        MarkUpdated(utcNow);
    }

    public void AsignarCoordenadas(double latitud, double longitud, DateTime utcNow)
    {
        EnsureCoordenadasValidas(latitud, longitud);

        Latitud = latitud;
        Longitud = longitud;
        MarkUpdated(utcNow);
    }

    public bool TieneCoordenadas() => Latitud.HasValue && Longitud.HasValue;

    public bool EsInicial() => OrdenParada == ReglasParada.OrdenInicial;

    private static void EnsureCoordenadasValidas(double? latitud, double? longitud)
    {
        if (latitud.HasValue != longitud.HasValue)
        {
            throw new DomainValidationException(
                "parada.coordenadas",
                ["La latitud y la longitud deben informarse juntas o no informarse."]);
        }

        if (latitud is < -90 or > 90)
        {
            throw new DomainValidationException(
                "parada.coordenadas",
                ["La latitud debe estar entre -90 y 90."]);
        }

        if (longitud is < -180 or > 180)
        {
            throw new DomainValidationException(
                "parada.coordenadas",
                ["La longitud debe estar entre -180 y 180."]);
        }
    }
}
