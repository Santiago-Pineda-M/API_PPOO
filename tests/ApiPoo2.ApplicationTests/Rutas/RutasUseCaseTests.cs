using ApiPoo2.Application.Exceptions;
using ApiPoo2.Application.UseCases.Rutas;
using ApiPoo2.Application.UseCases.Rutas.Create;
using ApiPoo2.Application.UseCases.Rutas.Delete;
using ApiPoo2.Application.UseCases.Rutas.Get;
using ApiPoo2.Application.UseCases.Rutas.List;
using ApiPoo2.Application.UseCases.Rutas.Update;
using ApiPoo2.ApplicationTests.Common.TestDoubles;
using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.TiposDocumento;
using ApiPoo2.Domain.Vehiculos;
using FluentAssertions;

namespace ApiPoo2.ApplicationTests.Rutas;

public sealed class RutasUseCaseTests
{
    private static readonly DateTime Ahora = TestData.Ahora;

    private sealed record Escenario(
        FakeRutaRepository Rutas,
        FakePersonaRepository Personas,
        FakeVehiculoRepository Vehiculos,
        FakeConductorVehiculoRepository Conductores,
        FakeUnitOfWork UnitOfWork,
        Persona Persona,
        Vehiculo Vehiculo);

    private static Escenario CrearEscenario()
    {
        var documento = DominioDePrueba.TipoDocumento();
        var persona = DominioDePrueba.ConductorConLicencia();
        var vehiculo = DominioDePrueba.VehiculoConDocumentos(documento);
        var asociacion = DominioDePrueba.AsociarOperable(persona, vehiculo);

        var personas = new FakePersonaRepository();
        personas.Add(persona);

        var vehiculos = new FakeVehiculoRepository();
        vehiculos.Add(vehiculo);

        var conductores = new FakeConductorVehiculoRepository();
        conductores.Items.Add(asociacion);

        return new Escenario(
            new FakeRutaRepository(),
            personas,
            vehiculos,
            conductores,
            new FakeUnitOfWork(),
            persona,
            vehiculo);
    }

    private static CreateRutaUseCase CrearCreate(Escenario escenario)
        => new(
            [new CreateRutaValidator()],
            TestData.LoggerFactory,
            escenario.Rutas,
            escenario.Personas,
            escenario.Vehiculos,
            escenario.Conductores,
            new FakeDateTimeProvider { UtcNow = Ahora },
            escenario.UnitOfWork);

    private static UpdateRutaUseCase CrearUpdate(Escenario escenario)
        => new(
            [new UpdateRutaValidator()],
            TestData.LoggerFactory,
            escenario.Rutas,
            escenario.Personas,
            escenario.Vehiculos,
            escenario.Conductores,
            new FakeDateTimeProvider { UtcNow = Ahora },
            escenario.UnitOfWork);

    private static CreateRutaInputDto Entrada(Escenario escenario, string codigo = "R-001")
        => new(
            codigo,
            escenario.Persona.Id,
            escenario.Vehiculo.Id,
            "Terminal de Ibagué",
            null,
            null,
            "admin");

    [Fact]
    public async Task CreateRuta_RutaOperable_CreaLaParadaInicialYPersiste()
    {
        var escenario = CrearEscenario();
        var useCase = CrearCreate(escenario);

        var resultado = await useCase.ExecuteAsync(Entrada(escenario), CancellationToken.None);

        resultado.CodigoRuta.Should().Be("R-001");
        resultado.NumeroIdentificacionConductor.Should().Be(escenario.Persona.NumeroIdentificacion.Value);
        resultado.Conductor.Should().Be("Carlos Ríos");
        resultado.Placa.Should().Be(escenario.Vehiculo.Placa.Value);
        resultado.LoginUsuario.Should().Be("admin");
        resultado.Operable.Should().BeTrue();
        resultado.MotivoNoOperable.Should().BeNull();
        resultado.Paradas.Should().ContainSingle()
            .Which.OrdenParada.Should().Be(0);
        escenario.Rutas.Items.Should().ContainSingle();
        escenario.UnitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task CreateRuta_CodigoDuplicado_RechazaConConflict()
    {
        var escenario = CrearEscenario();
        escenario.Rutas.Add(DominioDePrueba.RutaOperable("R-001", "Terminal"));

        var act = () => CrearCreate(escenario).ExecuteAsync(Entrada(escenario), CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>())
            .Which.Code.Should().Be("ruta.codigo.conflict");
    }

    [Fact]
    public async Task CreateRuta_PersonaInexistente_RechazaConNotFound()
    {
        var escenario = CrearEscenario();
        escenario.Personas.Items.Clear();

        var act = () => CrearCreate(escenario).ExecuteAsync(Entrada(escenario), CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>())
            .Which.Code.Should().Be("person.not_found");
    }

    [Fact]
    public async Task CreateRuta_VehiculoInexistente_RechazaConNotFound()
    {
        var escenario = CrearEscenario();
        escenario.Vehiculos.Items.Clear();

        var act = () => CrearCreate(escenario).ExecuteAsync(Entrada(escenario), CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>())
            .Which.Code.Should().Be("vehicle.not_found");
    }

    [Fact]
    public async Task CreateRuta_PersonaNoConductora_RechazaConForbidden()
    {
        var escenario = CrearEscenario();
        var administrativo = Persona.Register(
            TipoIdentificacion.CedulaCiudadania,
            "9999999999",
            "Ana",
            "Gómez",
            "ana@example.com",
            TipoPersona.Administrativo,
            Ahora);

        escenario.Personas.Items.Clear();
        escenario.Personas.Add(administrativo);

        var act = () => CrearCreate(escenario).ExecuteAsync(
            new CreateRutaInputDto(
                "R-001",
                administrativo.Id,
                escenario.Vehiculo.Id,
                "Terminal",
                null,
                null,
                "admin"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<ForbiddenException>())
            .Which.Code.Should().Be("ruta.conductor.invalido");
    }

    [Fact]
    public async Task CreateRuta_LicenciaVencida_RechazaConConflict()
    {
        var escenario = CrearEscenario();
        var sinLicencia = DominioDePrueba.Conductor();
        escenario.Personas.Items.Clear();
        escenario.Personas.Add(sinLicencia);

        var act = () => CrearCreate(escenario).ExecuteAsync(
            new CreateRutaInputDto(
                "R-001",
                sinLicencia.Id,
                escenario.Vehiculo.Id,
                "Terminal",
                null,
                null,
                "admin"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>())
            .Which.Code.Should().Be("ruta.licencia.vencida");
    }

    [Fact]
    public async Task CreateRuta_ConductorNoHabilitado_RechazaConConflict()
    {
        var escenario = CrearEscenario();
        escenario.Conductores.Items.Clear();

        var act = () => CrearCreate(escenario).ExecuteAsync(Entrada(escenario), CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>())
            .Which.Code.Should().Be("ruta.conductor.no_operable");
    }

    [Fact]
    public async Task CreateRuta_VehiculoSinDocumentosHabilitados_RechazaConConflict()
    {
        var escenario = CrearEscenario();
        var sinDocumentos = DominioDePrueba.Vehiculo("XYZ987");
        escenario.Vehiculos.Items.Clear();
        escenario.Vehiculos.Add(sinDocumentos);
        var asociacion = ConductorVehiculo.Crear(
            escenario.Persona.Id,
            sinDocumentos.Id,
            Ahora,
            EstadoConductor.Po);
        escenario.Persona.AgregarAsociacion(asociacion);
        escenario.Conductores.Items.Clear();
        escenario.Conductores.Items.Add(asociacion);

        var act = () => CrearCreate(escenario).ExecuteAsync(
            new CreateRutaInputDto(
                "R-001",
                escenario.Persona.Id,
                sinDocumentos.Id,
                "Terminal",
                null,
                null,
                "admin"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>())
            .Which.Code.Should().Be("ruta.documentos.no_habilitados");
    }

    [Fact]
    public async Task CreateRuta_LoginVacio_FallaLaValidacion()
    {
        var escenario = CrearEscenario();
        var act = () => CrearCreate(escenario).ExecuteAsync(
            Entrada(escenario) with { LoginUsuario = "  " },
            CancellationToken.None);

        await act.Should().ThrowAsync<RequestValidationException>();
    }

    [Fact]
    public async Task ListRutas_FiltraPorCodigoPlacaIdentificacionYOperabilidad()
    {
        var escenario = CrearEscenario();
        var operable = DominioDePrueba.RutaOperable("R-001", "Terminal");
        var noOperable = DominioDePrueba.RutaOperable("R-002", "Plaza", "9876543210", "XYZ987");
        noOperable.Vehiculo.GetTiposDocumento().Single()
            .CambiarEstado(EstadoDocumento.EnVerificacion, Ahora.AddMinutes(1));
        escenario.Rutas.Items.Add(operable);
        escenario.Rutas.Items.Add(noOperable);

        var useCase = new ListRutasUseCase(
            [],
            TestData.LoggerFactory,
            escenario.Rutas,
            new FakeDateTimeProvider { UtcNow = Ahora });

        var porCodigo = await useCase.ExecuteAsync(
            new ListRutasInputDto(Codigo: "R-001"),
            CancellationToken.None);
        var porPlaca = await useCase.ExecuteAsync(
            new ListRutasInputDto(Placa: operable.Vehiculo.Placa.Value),
            CancellationToken.None);
        var porIdentificacion = await useCase.ExecuteAsync(
            new ListRutasInputDto(
                NumeroIdentificacion: operable.Persona.NumeroIdentificacion.Value),
            CancellationToken.None);
        var operables = await useCase.ExecuteAsync(
            new ListRutasInputDto(Operable: true),
            CancellationToken.None);
        var noOperables = await useCase.ExecuteAsync(
            new ListRutasInputDto(Operable: false),
            CancellationToken.None);

        porCodigo.Should().ContainSingle().Which.CodigoRuta.Should().Be("R-001");
        porPlaca.Should().ContainSingle().Which.CodigoRuta.Should().Be("R-001");
        porIdentificacion.Should().ContainSingle().Which.CodigoRuta.Should().Be("R-001");
        operables.Should().ContainSingle().Which.CodigoRuta.Should().Be("R-001");
        noOperables.Should().ContainSingle().Which.CodigoRuta.Should().Be("R-002");
        noOperables.Single().MotivoNoOperable.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetRuta_Inexistente_RechazaConNotFound()
    {
        var useCase = new GetRutaUseCase(
            [],
            TestData.LoggerFactory,
            new FakeRutaRepository(),
            new FakeDateTimeProvider());

        var act = () => useCase.ExecuteAsync(new GetRutaInputDto("R-404"), CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("ruta.not_found");
    }

    [Fact]
    public async Task GetRuta_Existente_DevuelveElDetalleConParadas()
    {
        var ruta = DominioDePrueba.RutaOperable("R-001", "Terminal");
        ruta.AgregarParada("Destino", 1, null, null, Ahora);
        var rutas = new FakeRutaRepository();
        rutas.Add(ruta);

        var useCase = new GetRutaUseCase(
            [],
            TestData.LoggerFactory,
            rutas,
            new FakeDateTimeProvider { UtcNow = Ahora });

        var resultado = await useCase.ExecuteAsync(new GetRutaInputDto("R-001"), CancellationToken.None);

        resultado.CodigoRuta.Should().Be("R-001");
        resultado.Paradas.Should().HaveCount(2);
        resultado.Operable.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateRuta_ReasignaConductorYVehiculo()
    {
        var escenario = CrearEscenario();
        var ruta = DominioDePrueba.RutaOperable("R-001", "Terminal");
        escenario.Rutas.Add(ruta);

        var resultado = await CrearUpdate(escenario).ExecuteAsync(
            new UpdateRutaInputDto("R-001", escenario.Persona.Id, escenario.Vehiculo.Id),
            CancellationToken.None);

        resultado.PersonaId.Should().Be(escenario.Persona.Id);
        resultado.VehiculoId.Should().Be(escenario.Vehiculo.Id);
        escenario.UnitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task UpdateRuta_Inexistente_RechazaConNotFound()
    {
        var escenario = CrearEscenario();

        var act = () => CrearUpdate(escenario).ExecuteAsync(
            new UpdateRutaInputDto("R-404", escenario.Persona.Id, escenario.Vehiculo.Id),
            CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("ruta.not_found");
    }

    [Fact]
    public async Task DeleteRuta_EliminaYPersiste()
    {
        var ruta = DominioDePrueba.RutaOperable("R-001", "Terminal");
        var rutas = new FakeRutaRepository();
        rutas.Add(ruta);
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new DeleteRutaUseCase([], TestData.LoggerFactory, rutas, unitOfWork);

        await useCase.ExecuteAsync(new DeleteRutaInputDto("R-001"), CancellationToken.None);

        rutas.Items.Should().BeEmpty();
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task DeleteRuta_Inexistente_RechazaConNotFound()
    {
        var useCase = new DeleteRutaUseCase(
            [],
            TestData.LoggerFactory,
            new FakeRutaRepository(),
            new FakeUnitOfWork());

        var act = () => useCase.ExecuteAsync(new DeleteRutaInputDto("R-404"), CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("ruta.not_found");
    }
}
