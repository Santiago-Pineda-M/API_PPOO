using ApiPoo2.Application.Common;
using ApiPoo2.Application.IServices;
using ApiPoo2.Application.UseCases.Trayectos.Jobs;
using ApiPoo2.ApplicationTests.Common.TestDoubles;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Rutas;
using ApiPoo2.Domain.TiposDocumento;
using ApiPoo2.Domain.Vehiculos;
using FluentAssertions;

namespace ApiPoo2.ApplicationTests.Trayectos;

public sealed class TrayectosJobsTests
{
    private static readonly DateTime Ahora = TestData.Ahora;

    [Fact]
    public async Task ExpirarLicencias_RestringeAlConductorYNotifica()
    {
        var persona = DominioDePrueba.ConductorConLicencia(vigencia: Ahora.AddHours(1));
        var vehiculo = DominioDePrueba.VehiculoConDocumentos(DominioDePrueba.TipoDocumento());
        DominioDePrueba.AsociarOperable(persona, vehiculo);

        var personas = new FakePersonaRepository();
        personas.Add(persona);
        var notifier = new FakeEmailNotifier();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new ExpirarLicenciasConductoresUseCase(
            [],
            TestData.LoggerFactory,
            personas,
            notifier,
            new FakeDateTimeProvider { UtcNow = Ahora.AddHours(2) },
            unitOfWork);

        var restringidos = await useCase.ExecuteAsync(new SinParametros(), CancellationToken.None);

        restringidos.Should().Be(1);
        persona.ConductoresVehiculosAssociated.Single().Estado.Should().Be(EstadoConductor.Ro);
        notifier.Notificaciones.Should().ContainSingle()
            .Which.Correo.Should().Be(persona.CorreoElectronico.Value);
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task ExpirarLicencias_SinAsociaciones_NoPersisteNiNotifica()
    {
        var persona = DominioDePrueba.ConductorConLicencia(vigencia: Ahora.AddHours(1));
        var personas = new FakePersonaRepository();
        personas.Add(persona);
        var notifier = new FakeEmailNotifier();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new ExpirarLicenciasConductoresUseCase(
            [],
            TestData.LoggerFactory,
            personas,
            notifier,
            new FakeDateTimeProvider { UtcNow = Ahora.AddHours(2) },
            unitOfWork);

        var restringidos = await useCase.ExecuteAsync(new SinParametros(), CancellationToken.None);

        restringidos.Should().Be(0);
        notifier.Notificaciones.Should().BeEmpty();
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task ExpirarLicencias_ConLicenciaVigente_NoRestringe()
    {
        var persona = DominioDePrueba.ConductorConLicencia(vigencia: Ahora.AddMonths(1));
        var vehiculo = DominioDePrueba.VehiculoConDocumentos(DominioDePrueba.TipoDocumento());
        DominioDePrueba.AsociarOperable(persona, vehiculo);

        var personas = new FakePersonaRepository();
        personas.Add(persona);
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new ExpirarLicenciasConductoresUseCase(
            [],
            TestData.LoggerFactory,
            personas,
            new FakeEmailNotifier(),
            new FakeDateTimeProvider { UtcNow = Ahora },
            unitOfWork);

        var restringidos = await useCase.ExecuteAsync(new SinParametros(), CancellationToken.None);

        restringidos.Should().Be(0);
        persona.ConductoresVehiculosAssociated.Single().Estado.Should().Be(EstadoConductor.Po);
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task MarcarDocumentosVencidos_PersisteElEstadoVencido()
    {
        var documento = DominioDePrueba.TipoDocumento();
        var vehiculo = DominioDePrueba.VehiculoConDocumentos(
            documento,
            vencimiento: Ahora.AddDays(5),
            estado: EstadoDocumento.Habilitado);
        var vehiculos = new FakeVehiculoRepository();
        vehiculos.Add(vehiculo);
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new MarcarDocumentosVencidosUseCase(
            [],
            TestData.LoggerFactory,
            vehiculos,
            new FakeDateTimeProvider { UtcNow = Ahora.AddDays(10) },
            unitOfWork);

        var marcados = await useCase.ExecuteAsync(new SinParametros(), CancellationToken.None);

        marcados.Should().Be(1);
        vehiculo.GetTiposDocumento().Single().Estado.Should().Be(EstadoDocumento.Vencido);
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task MarcarDocumentosVencidos_DocumentoVigente_NoLoMarca()
    {
        var vehiculo = DominioDePrueba.VehiculoConDocumentos(
            DominioDePrueba.TipoDocumento(),
            vencimiento: Ahora.AddDays(30),
            estado: EstadoDocumento.Habilitado);
        var vehiculos = new FakeVehiculoRepository();
        vehiculos.Add(vehiculo);
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new MarcarDocumentosVencidosUseCase(
            [],
            TestData.LoggerFactory,
            vehiculos,
            new FakeDateTimeProvider { UtcNow = Ahora },
            unitOfWork);

        var marcados = await useCase.ExecuteAsync(new SinParametros(), CancellationToken.None);

        marcados.Should().Be(0);
        vehiculo.GetTiposDocumento().Single().Estado.Should().Be(EstadoDocumento.Habilitado);
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task MarcarDocumentosVencidos_YaVencido_NoLoCuentaDeNuevo()
    {
        var vehiculo = DominioDePrueba.VehiculoConDocumentos(
            DominioDePrueba.TipoDocumento(),
            vencimiento: Ahora.AddDays(5),
            estado: EstadoDocumento.Habilitado);
        vehiculo.GetTiposDocumento().Single().MarcarVencido(Ahora.AddDays(10));
        var vehiculos = new FakeVehiculoRepository();
        vehiculos.Add(vehiculo);

        var useCase = new MarcarDocumentosVencidosUseCase(
            [],
            TestData.LoggerFactory,
            vehiculos,
            new FakeDateTimeProvider { UtcNow = Ahora.AddDays(10) },
            new FakeUnitOfWork());

        var marcados = await useCase.ExecuteAsync(new SinParametros(), CancellationToken.None);

        marcados.Should().Be(0);
    }

    private static (FakeRutaRepository Rutas, Ruta Ruta, FakeUnitOfWork UnitOfWork) EscenarioGeocodificacion()
    {
        var ruta = DominioDePrueba.RutaOperable("R-001", "Terminal de Ibagué");
        var rutas = new FakeRutaRepository();
        rutas.Add(ruta);
        return (rutas, ruta, new FakeUnitOfWork());
    }

    private static GeocodificarParadasUseCase CrearGeocodificar(
        FakeRutaRepository rutas,
        FakeGeocodingService geocoding,
        FakeUnitOfWork unitOfWork)
        => new(
            [],
            TestData.LoggerFactory,
            rutas,
            geocoding,
            new FakeDateTimeProvider { UtcNow = Ahora },
            unitOfWork,
            new GeocodingOptions { PausaEntreLlamadasMs = 0, MaxPorCiclo = 10 });

    [Fact]
    public async Task GeocodificarParadas_AsignaLasCoordenadasEncontradas()
    {
        var (rutas, ruta, unitOfWork) = EscenarioGeocodificacion();
        var geocoding = new FakeGeocodingService();
        geocoding.Coordenadas["Terminal de Ibagué"] = new Coordenadas(4.4389, -75.2322);

        var asignadas = await CrearGeocodificar(rutas, geocoding, unitOfWork)
            .ExecuteAsync(new SinParametros(), CancellationToken.None);

        asignadas.Should().Be(1);
        var geocodificada = ruta.Paradas.Single(p => p.Ubicacion.Value == "Terminal de Ibagué");
        geocodificada.Latitud.Should().Be(4.4389);
        geocodificada.Longitud.Should().Be(-75.2322);
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task GeocodificarParadas_SinResultado_NoPersiste()
    {
        var (rutas, ruta, unitOfWork) = EscenarioGeocodificacion();

        var asignadas = await CrearGeocodificar(rutas, new FakeGeocodingService(), unitOfWork)
            .ExecuteAsync(new SinParametros(), CancellationToken.None);

        asignadas.Should().Be(0);
        ruta.Paradas.Should().OnlyContain(p => !p.TieneCoordenadas());
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task GeocodificarParadas_ElServicioFalla_ContinuaConLasDemas()
    {
        var ruta = DominioDePrueba.RutaOperable("R-001", "Terminal");
        ruta.AgregarParada("Plaza", 2, null, null, Ahora);
        ruta.AgregarParada("Mercado", 3, null, null, Ahora);
        var rutas = new FakeRutaRepository();
        rutas.Add(ruta);

        var geocoding = new FakeGeocodingService
        {
            ThrowOnGeocode = new HttpRequestException("sin red"),
        };

        var asignadas = await CrearGeocodificar(rutas, geocoding, new FakeUnitOfWork())
            .ExecuteAsync(new SinParametros(), CancellationToken.None);

        asignadas.Should().Be(0);
        geocoding.Consultas.Should().HaveCount(4);
    }

    [Fact]
    public async Task GeocodificarParadas_SinPendientes_NoConsultaElServicio()
    {
        var ruta = DominioDePrueba.RutaOperable("R-001", "Terminal");

        foreach (var parada in ruta.Paradas.ToList())
        {
            ruta.AsignarCoordenadas(parada.Id, 4.4, -75.2, Ahora);
        }

        var rutas = new FakeRutaRepository();
        rutas.Add(ruta);
        var geocoding = new FakeGeocodingService();
        var unitOfWork = new FakeUnitOfWork();

        var asignadas = await CrearGeocodificar(rutas, geocoding, unitOfWork)
            .ExecuteAsync(new SinParametros(), CancellationToken.None);

        asignadas.Should().Be(0);
        geocoding.Consultas.Should().BeEmpty();
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }
}
