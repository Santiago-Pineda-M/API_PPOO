using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Rutas;
using FluentAssertions;

namespace ApiPoo2.DomainTests.Domain;

public sealed class RutaTests
{
    private static readonly DateTime Ahora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Guid PersonaId = Guid.NewGuid();

    private static readonly Guid VehiculoId = Guid.NewGuid();

    private static Ruta CrearRuta(string codigo = "R-001")
        => Ruta.Registrar(
            codigo,
            PersonaId,
            VehiculoId,
            "admin",
            "Terminal de Ibagué",
            null,
            null,
            "Aeropuerto Perales",
            null,
            null,
            Ahora);

    [Fact]
    public void Registrar_CreaLaParadaInicialYLaFinal()
    {
        var ruta = CrearRuta();

        ruta.Codigo.Value.Should().Be("R-001");
        ruta.PersonaId.Should().Be(PersonaId);
        ruta.VehiculoId.Should().Be(VehiculoId);
        ruta.LoginUsuario.Value.Should().Be("admin");
        ruta.CantidadParadas().Should().Be(2);
        ruta.Paradas.Single(p => p.EsInicial()).Ubicacion.Value.Should().Be("Terminal de Ibagué");
        ruta.Paradas.Single(p => p.OrdenParada == ReglasParada.OrdenFinalMinimo)
            .Ubicacion.Value.Should().Be("Aeropuerto Perales");
        ruta.CreatedAtUtc.Should().Be(Ahora);
        ruta.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Registrar_RechazaParadaFinalVacia()
        => FluentActions.Invoking(() => Ruta.Registrar(
                "R-001",
                PersonaId,
                VehiculoId,
                "admin",
                "Terminal",
                null,
                null,
                "  ",
                null,
                null,
                Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("parada.ubicacion.requerida");

    [Fact]
    public void Registrar_NormalizaElCodigoAMayusculas()
    {
        var ruta = CrearRuta("  r-001  ");

        ruta.Codigo.Value.Should().Be("R-001");
    }

    [Fact]
    public void Registrar_RechazaConductorVacio()
        => FluentActions.Invoking(() => Ruta.Registrar(
                "R-001",
                Guid.Empty,
                VehiculoId,
                "admin",
                "Terminal",
                null,
                null,
                "Destino",
                null,
                null,
                Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("ruta.conductor");

    [Fact]
    public void Registrar_RechazaVehiculoVacio()
        => FluentActions.Invoking(() => Ruta.Registrar(
                "R-001",
                PersonaId,
                Guid.Empty,
                "admin",
                "Terminal",
                null,
                null,
                "Destino",
                null,
                null,
                Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("ruta.vehiculo");

    [Fact]
    public void AgregarParada_IncrementaElTotalYActualizaLaRuta()
    {
        var ruta = CrearRuta();

        var parada = ruta.AgregarParada("Plaza de Bolívar", 2, 4.4389, -75.2322, Ahora.AddMinutes(1));

        ruta.CantidadParadas().Should().Be(3);
        parada.OrdenParada.Should().Be(2);
        parada.Latitud.Should().Be(4.4389);
        parada.Longitud.Should().Be(-75.2322);
        ruta.UpdatedAtUtc.Should().Be(Ahora.AddMinutes(1));
    }

    [Fact]
    public void AgregarParada_RechazaOrdenDuplicado()
    {
        var ruta = CrearRuta();

        var act = () => ruta.AgregarParada("Otra", ReglasParada.OrdenInicial, null, null, Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("parada.orden.duplicado");
    }

    [Fact]
    public void AgregarParada_RechazaOrdenFueraDeRango()
    {
        var ruta = CrearRuta();

        var act = () => ruta.AgregarParada("Otra", 7, null, null, Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("parada.orden.rango");
    }

    [Fact]
    public void AgregarParada_RechazaCuandoYaHaySieteParadas()
    {
        var ruta = CrearRuta();

        for (var orden = 2; orden <= ReglasParada.OrdenMaximo; orden++)
        {
            ruta.AgregarParada($"Parada {orden}", orden, null, null, Ahora);
        }

        var act = () => ruta.AgregarParada("Extra", 6, null, null, Ahora);

        ruta.CantidadParadas().Should().Be(ReglasParada.MaximoParadasPorRuta);
        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("parada.maximo");
    }

    [Fact]
    public void ActualizarParada_CambiaUbicacionOrdenYCoordenadas()
    {
        var ruta = CrearRuta();
        var parada = ruta.AgregarParada("Plaza", 2, null, null, Ahora);

        ruta.ActualizarParada(parada.Id, "Mercado", 3, 4.5, -75.2, Ahora.AddMinutes(2));

        parada.Ubicacion.Value.Should().Be("Mercado");
        parada.OrdenParada.Should().Be(3);
        parada.Latitud.Should().Be(4.5);
        ruta.UpdatedAtUtc.Should().Be(Ahora.AddMinutes(2));
    }

    [Fact]
    public void ActualizarParada_RechazaParadaInexistente()
    {
        var ruta = CrearRuta();

        var act = () => ruta.ActualizarParada(Guid.NewGuid(), "X", 1, null, null, Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("parada.not_found");
    }

    [Fact]
    public void ActualizarParada_RechazaOrdenOcupadoPorOtraParada()
    {
        var ruta = CrearRuta();
        var segunda = ruta.AgregarParada("Plaza", 2, null, null, Ahora);

        var act = () => ruta.ActualizarParada(segunda.Id, "Mercado", ReglasParada.OrdenInicial, null, null, Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("parada.orden.duplicado");
    }

    [Fact]
    public void EliminarParada_QuitaUnaParadaNoInicial()
    {
        var ruta = CrearRuta();
        var parada = ruta.AgregarParada("Plaza", 2, null, null, Ahora);

        ruta.EliminarParada(parada.Id, Ahora.AddMinutes(1));

        ruta.CantidadParadas().Should().Be(2);
        ruta.BuscarParada(parada.Id).Should().BeNull();
    }

    [Fact]
    public void EliminarParada_RechazaLaParadaInicial()
    {
        var ruta = CrearRuta();
        var inicial = ruta.Paradas.Single(p => p.EsInicial());

        var act = () => ruta.EliminarParada(inicial.Id, Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("parada.inicial.requerida");
    }

    [Fact]
    public void EliminarParada_RechazaParadaInexistente()
    {
        var ruta = CrearRuta();

        var act = () => ruta.EliminarParada(Guid.NewGuid(), Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("parada.not_found");
    }

    [Fact]
    public void AsignarCoordenadas_ActualizaLaParadaYLaRuta()
    {
        var ruta = CrearRuta();
        var parada = ruta.Paradas.First();

        ruta.AsignarCoordenadas(parada.Id, 4.4389, -75.2322, Ahora.AddMinutes(5));

        parada.TieneCoordenadas().Should().BeTrue();
        parada.Latitud.Should().Be(4.4389);
        ruta.UpdatedAtUtc.Should().Be(Ahora.AddMinutes(5));
    }

    [Fact]
    public void AsignarCoordenadas_RechazaParadaInexistente()
    {
        var ruta = CrearRuta();

        var act = () => ruta.AsignarCoordenadas(Guid.NewGuid(), 4.4, -75.2, Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("parada.not_found");
    }

    [Fact]
    public void Reasignar_CambiaConductorYVehiculo()
    {
        var ruta = CrearRuta();
        var otroConductor = Guid.NewGuid();
        var otroVehiculo = Guid.NewGuid();

        ruta.Reasignar(otroConductor, otroVehiculo, Ahora.AddMinutes(1));

        ruta.PersonaId.Should().Be(otroConductor);
        ruta.VehiculoId.Should().Be(otroVehiculo);
        ruta.UpdatedAtUtc.Should().Be(Ahora.AddMinutes(1));
    }

    [Fact]
    public void Reasignar_MismosValores_NoActualizaLaFecha()
    {
        var ruta = CrearRuta();

        ruta.Reasignar(PersonaId, VehiculoId, Ahora.AddDays(1));

        ruta.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Reasignar_RechazaConductorVacio()
    {
        var ruta = CrearRuta();

        var act = () => ruta.Reasignar(Guid.Empty, VehiculoId, Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("ruta.conductor");
    }

    [Fact]
    public void Reasignar_RechazaVehiculoVacio()
    {
        var ruta = CrearRuta();

        var act = () => ruta.Reasignar(PersonaId, Guid.Empty, Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("ruta.vehiculo");
    }

    [Fact]
    public void EstaCompleta_EsVerdaderaDesdeElRegistroDeLaRuta()
    {
        var ruta = CrearRuta();

        ruta.EstaCompleta().Should().BeTrue();
    }

    [Fact]
    public void EstaCompleta_EsFalsaSiSoloQuedaLaParadaInicial()
    {
        var ruta = CrearRuta();
        var final = ruta.Paradas.Single(p => !p.EsInicial());

        ruta.EliminarParada(final.Id, Ahora.AddMinutes(1));

        ruta.EstaCompleta().Should().BeFalse();
    }

    [Fact]
    public void BuscarParada_ConIdentificadorDesconocido_DevuelveNulo()
        => CrearRuta().BuscarParada(Guid.NewGuid()).Should().BeNull();
}
