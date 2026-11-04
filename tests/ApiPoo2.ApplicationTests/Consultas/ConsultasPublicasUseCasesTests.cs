using ApiPoo2.Application.Exceptions;
using ApiPoo2.ApplicationTests.Common.TestDoubles;
using ApiPoo2.Application.UseCases.Consultas;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.TiposDocumento;
using FluentAssertions;

namespace ApiPoo2.ApplicationTests.Consultas;

public sealed class ConsultasPublicasUseCasesTests
{
    private static readonly DateTime Ahora = TestData.Ahora;

    private static DocumentoVehiculo CrearDocumentoVehiculo(
        DateTime? vencimiento = null,
        DateTime? expedicion = null)
        => DocumentoVehiculo.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TestData.PdfValido,
            "SOAT.pdf",
            expedicion ?? Ahora.AddDays(-10),
            vencimiento ?? Ahora.AddDays(30),
            Ahora);

    [Fact]
    public async Task ConductoresOperables_SoloDevuelveLosQuePuedenOperar()
    {
        var repository = new FakeConductorVehiculoRepository();
        var operativo = ConductorVehiculo.Crear(Guid.NewGuid(), Guid.NewGuid(), Ahora, EstadoConductor.Po);
        var enEspera = ConductorVehiculo.Crear(Guid.NewGuid(), Guid.NewGuid(), Ahora, EstadoConductor.Ea);
        var restringido = ConductorVehiculo.Crear(Guid.NewGuid(), Guid.NewGuid(), Ahora, EstadoConductor.Ro);
        repository.Add(operativo);
        repository.Add(enEspera);
        repository.Add(restringido);

        var useCase = new GetConductoresOperablesUseCase(
            [], TestData.LoggerFactory, repository);

        var resultado = await useCase.ExecuteAsync(
            new GetConductoresOperablesInputDto(10),
            CancellationToken.None);

        resultado.Should().ContainSingle();
        resultado[0].VehiculoId.Should().Be(operativo.VehiculoId);
    }

    [Fact]
    public async Task ConductoresOperables_LimitaPorCantidad()
    {
        var repository = new FakeConductorVehiculoRepository();
        for (var i = 0; i < 5; i++)
        {
            repository.Add(ConductorVehiculo.Crear(Guid.NewGuid(), Guid.NewGuid(), Ahora, EstadoConductor.Po));
        }

        var useCase = new GetConductoresOperablesUseCase(
            [], TestData.LoggerFactory, repository);

        var resultado = await useCase.ExecuteAsync(
            new GetConductoresOperablesInputDto(2),
            CancellationToken.None);

        resultado.Should().HaveCount(2);
    }

    [Fact]
    public async Task ConductoresOperables_ExponeLosNombresDeLaPersona()
    {
        var persona = Persona.Register(
            TipoIdentificacion.CedulaCiudadania, "1", "Juan", "Pérez",
            "juan@example.com", TipoPersona.Conductor, Ahora);

        var repository = new FakeConductorVehiculoRepository();
        repository.Add(
            ConductorVehiculo.Crear(persona.Id, Guid.NewGuid(), Ahora, EstadoConductor.Po)
                .ConPersona(persona));

        var useCase = new GetConductoresOperablesUseCase(
            [], TestData.LoggerFactory, repository);

        var resultado = await useCase.ExecuteAsync(
            new GetConductoresOperablesInputDto(10),
            CancellationToken.None);

        resultado.Should().ContainSingle();
        resultado[0].Nombres.Should().Be("Juan");
        resultado[0].Apellidos.Should().Be("Pérez");
        resultado[0].PersonaId.Should().Be(persona.Id);
    }

    [Fact]
    public async Task ConductoresOperables_SinPersonaCargadaDevuelveNombresVacios()
    {
        var repository = new FakeConductorVehiculoRepository();
        repository.Add(ConductorVehiculo.Crear(Guid.NewGuid(), Guid.NewGuid(), Ahora, EstadoConductor.Po));

        var useCase = new GetConductoresOperablesUseCase(
            [], TestData.LoggerFactory, repository);

        var resultado = await useCase.ExecuteAsync(
            new GetConductoresOperablesInputDto(10),
            CancellationToken.None);

        resultado.Should().ContainSingle();
        resultado[0].Nombres.Should().BeEmpty();
        resultado[0].Apellidos.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task DocumentosPorVencer_RechazaDiasNoPositivos(int dias)
    {
        var useCase = new GetDocumentosPorVencerUseCase(
            [], TestData.LoggerFactory, new FakeTipoDocumentoRepository(), new FakeDateTimeProvider());

        var act = () => useCase.ExecuteAsync(
            new GetDocumentosPorVencerInputDto(dias),
            CancellationToken.None);

        await act.Should().ThrowAsync<RequestValidationException>();
    }

    [Fact]
    public async Task DocumentosPorVencer_OrdenaPorFechaDeVencimiento()
    {
        var repository = new FakeTipoDocumentoRepository();
        var lejano = CrearDocumentoVehiculo(Ahora.AddDays(20));
        var cercano = CrearDocumentoVehiculo(Ahora.AddDays(5));
        repository.DocumentosVehiculo.Add(cercano);
        repository.DocumentosVehiculo.Add(lejano);

        var useCase = new GetDocumentosPorVencerUseCase(
            [], TestData.LoggerFactory, repository, new FakeDateTimeProvider { UtcNow = Ahora });

        var resultado = await useCase.ExecuteAsync(
            new GetDocumentosPorVencerInputDto(30),
            CancellationToken.None);

        resultado.Should().HaveCount(2);
        resultado[0].TipoDocumentoId.Should().Be(cercano.TipoDocumentoId);
        resultado[1].TipoDocumentoId.Should().Be(lejano.TipoDocumentoId);
        repository.UltimosDiasConsultados.Should().Be(30);
        repository.UltimoUtcNowConsultado.Should().Be(Ahora);
    }

    [Fact]
    public async Task DocumentosPorVencer_DerivaVencidoPorFecha()
    {
        var repository = new FakeTipoDocumentoRepository();
        repository.DocumentosVehiculo.Add(CrearDocumentoVehiculo(Ahora.AddDays(-1)));

        var useCase = new GetDocumentosPorVencerUseCase(
            [], TestData.LoggerFactory, repository, new FakeDateTimeProvider { UtcNow = Ahora });

        var resultado = await useCase.ExecuteAsync(
            new GetDocumentosPorVencerInputDto(30),
            CancellationToken.None);

        resultado.Should().ContainSingle();
        resultado[0].Estado.Should().Be(EstadoDocumento.Vencido);
    }

    [Fact]
    public async Task DocumentosVencidos_TodosSeReportanComoVencidos()
    {
        var repository = new FakeTipoDocumentoRepository();
        var vencido = CrearDocumentoVehiculo(Ahora.AddDays(-1));
        repository.DocumentosVehiculo.Add(vencido);
        repository.DocumentosVehiculo.Add(CrearDocumentoVehiculo(Ahora.AddDays(30)));

        var useCase = new GetVehiculosDocumentosVencidosUseCase(
            [], TestData.LoggerFactory, repository, new FakeDateTimeProvider { UtcNow = Ahora });

        var resultado = await useCase.ExecuteAsync(
            new GetVehiculosDocumentosVencidosInputDto(),
            CancellationToken.None);

        resultado.Should().ContainSingle();
        resultado[0].TipoDocumentoId.Should().Be(vencido.TipoDocumentoId);
        resultado[0].Estado.Should().Be(EstadoDocumento.Vencido);
    }

    [Fact]
    public async Task PersonasPorTipo_IncluyeTodosLosTiposInclusoEnCero()
    {
        var repository = new FakePersonaRepository();
        repository.Add(Persona.Register(
            TipoIdentificacion.CedulaCiudadania, "1", "Root", "Semilla",
            "root@example.com", TipoPersona.Administrativo, Ahora));
        repository.Add(Persona.Register(
            TipoIdentificacion.CedulaCiudadania, "2", "Ana", "Gómez",
            "ana@example.com", TipoPersona.Conductor, Ahora));
        repository.Add(Persona.Register(
            TipoIdentificacion.CedulaCiudadania, "3", "Luis", "Díaz",
            "luis@example.com", TipoPersona.Conductor, Ahora));

        var useCase = new CountPersonasByTipoUseCase(
            [], TestData.LoggerFactory, repository);

        var resultado = await useCase.ExecuteAsync(
            new CountPersonasByTipoInputDto(),
            CancellationToken.None);

        resultado.Should().HaveCount(Enum.GetValues<TipoPersona>().Length);
        resultado.Should().Contain(p => p.Tipo == TipoPersona.Administrativo && p.Cantidad == 1);
        resultado.Should().Contain(p => p.Tipo == TipoPersona.Conductor && p.Cantidad == 2);
    }

    [Fact]
    public async Task PersonasPorTipo_SinPersonasDevuelveCeroEnTodosLosTipos()
    {
        var useCase = new CountPersonasByTipoUseCase(
            [], TestData.LoggerFactory, new FakePersonaRepository());

        var resultado = await useCase.ExecuteAsync(
            new CountPersonasByTipoInputDto(),
            CancellationToken.None);

        resultado.Should().NotBeEmpty();
        resultado.Should().OnlyContain(p => p.Cantidad == 0);
    }
}
