using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Rutas;
using FluentAssertions;

namespace ApiPoo2.DomainTests.Domain;

public sealed class ParadaTests
{
    private static readonly DateTime Ahora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Guid RutaId = Guid.NewGuid();

    private static Parada Crear(
        int orden = 1,
        double? latitud = null,
        double? longitud = null,
        string ubicacion = "Plaza de Bolívar")
        => Parada.Crear(RutaId, ubicacion, orden, latitud, longitud, Ahora);

    [Fact]
    public void Crear_AsignaCamposYEstadoInicial()
    {
        var parada = Crear(2, 4.4389, -75.2322);

        parada.IdRuta.Should().Be(RutaId);
        parada.OrdenParada.Should().Be(2);
        parada.Ubicacion.Value.Should().Be("Plaza de Bolívar");
        parada.Latitud.Should().Be(4.4389);
        parada.Longitud.Should().Be(-75.2322);
        parada.TieneCoordenadas().Should().BeTrue();
        parada.EsInicial().Should().BeFalse();
        parada.CreatedAtUtc.Should().Be(Ahora);
    }

    [Fact]
    public void Crear_RechazaRutaVacia()
    {
        var act = () => Parada.Crear(Guid.Empty, "Plaza", 1, null, null, Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("parada.ruta");
    }

    [Fact]
    public void Crear_RechazaOrdenFueraDeRango()
    {
        var act = () => Parada.Crear(RutaId, "Plaza", 9, null, null, Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("parada.orden.rango");
    }

    [Fact]
    public void Crear_RechazaCoordenadasParciales()
    {
        var act = () => Parada.Crear(RutaId, "Plaza", 1, 4.4, null, Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("parada.coordenadas");
    }

    [Fact]
    public void Crear_RechazaLatitudFueraDeRango()
    {
        var act = () => Parada.Crear(RutaId, "Plaza", 1, 100, -75, Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("parada.coordenadas");
    }

    [Fact]
    public void Crear_RechazaLongitudFueraDeRango()
    {
        var act = () => Parada.Crear(RutaId, "Plaza", 1, 4.4, -200, Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("parada.coordenadas");
    }

    [Fact]
    public void Actualizar_ReemplazaUbicacionYOrden()
    {
        var parada = Crear();

        parada.Actualizar("Mercado", 3, 4.4, -75.2, Ahora.AddMinutes(1));

        parada.Ubicacion.Value.Should().Be("Mercado");
        parada.OrdenParada.Should().Be(3);
        parada.UpdatedAtUtc.Should().Be(Ahora.AddMinutes(1));
    }

    [Fact]
    public void Actualizar_RechazaCoordenadasParciales()
    {
        var parada = Crear();

        var act = () => parada.Actualizar("Mercado", 3, null, -75.2, Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("parada.coordenadas");
    }

    [Fact]
    public void AsignarCoordenadas_MarcaLaParadaGeocodificada()
    {
        var parada = Crear();

        parada.AsignarCoordenadas(4.4389, -75.2322, Ahora.AddMinutes(2));

        parada.TieneCoordenadas().Should().BeTrue();
        parada.Latitud.Should().Be(4.4389);
        parada.Longitud.Should().Be(-75.2322);
        parada.UpdatedAtUtc.Should().Be(Ahora.AddMinutes(2));
    }

    [Fact]
    public void EsInicial_SoloEsCiertoParaElOrdenCero()
    {
        Crear(ReglasParada.OrdenInicial).EsInicial().Should().BeTrue();
        Crear(1).EsInicial().Should().BeFalse();
    }
}
