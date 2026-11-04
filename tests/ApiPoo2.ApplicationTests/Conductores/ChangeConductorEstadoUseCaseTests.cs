using ApiPoo2.Application.Exceptions;
using ApiPoo2.ApplicationTests.Common.TestDoubles;
using ApiPoo2.Application.UseCases.Conductores;
using ApiPoo2.Domain.Personas;
using FluentAssertions;

namespace ApiPoo2.ApplicationTests.Conductores;

public sealed class ChangeConductorEstadoUseCaseTests
{
    private static readonly DateTime Ahora = TestData.Ahora;

    private static ChangeConductorEstadoUseCase CrearUseCase(
        FakeConductorVehiculoRepository repository,
        FakeUnitOfWork? unitOfWork = null)
        => new(
            [],
            TestData.LoggerFactory,
            repository,
            new FakeDateTimeProvider { UtcNow = Ahora },
            unitOfWork ?? new FakeUnitOfWork());

    [Fact]
    public async Task CambioDeEstado_PersisteYMarcaActualizado()
    {
        var personaId = Guid.NewGuid();
        var vehiculoId = Guid.NewGuid();
        var repository = new FakeConductorVehiculoRepository();
        repository.Add(ConductorVehiculo.Crear(personaId, vehiculoId, Ahora));
        var unitOfWork = new FakeUnitOfWork();

        var resultado = await CrearUseCase(repository, unitOfWork).ExecuteAsync(
            new ChangeConductorEstadoInputDto(personaId, vehiculoId, EstadoConductor.Po),
            CancellationToken.None);

        resultado.Succeeded.Should().BeTrue();
        repository.Items[0].Estado.Should().Be(EstadoConductor.Po);
        repository.Items[0].PuedeOperar().Should().BeTrue();
        repository.Items[0].UpdatedAtUtc.Should().Be(Ahora);
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task CambioDeEstado_AOtroEstadoRestringido()
    {
        var personaId = Guid.NewGuid();
        var vehiculoId = Guid.NewGuid();
        var repository = new FakeConductorVehiculoRepository();
        repository.Add(ConductorVehiculo.Crear(personaId, vehiculoId, Ahora, EstadoConductor.Po));

        await CrearUseCase(repository).ExecuteAsync(
            new ChangeConductorEstadoInputDto(personaId, vehiculoId, EstadoConductor.Ro),
            CancellationToken.None);

        repository.Items[0].Estado.Should().Be(EstadoConductor.Ro);
        repository.Items[0].PuedeOperar().Should().BeFalse();
    }

    [Fact]
    public async Task CambioAlMismoEstado_NoMarcaActualizado()
    {
        var personaId = Guid.NewGuid();
        var vehiculoId = Guid.NewGuid();
        var repository = new FakeConductorVehiculoRepository();
        repository.Add(ConductorVehiculo.Crear(personaId, vehiculoId, Ahora, EstadoConductor.Po));

        await CrearUseCase(repository).ExecuteAsync(
            new ChangeConductorEstadoInputDto(personaId, vehiculoId, EstadoConductor.Po),
            CancellationToken.None);

        repository.Items[0].UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task AsociaciónInexistente_RechazaConNotFound()
    {
        var useCase = CrearUseCase(new FakeConductorVehiculoRepository());

        var act = () => useCase.ExecuteAsync(
            new ChangeConductorEstadoInputDto(Guid.NewGuid(), Guid.NewGuid(), EstadoConductor.Po),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<NotFoundException>();
        exception.Which.Code.Should().Be("conductor.association.not_found");
    }

    [Fact]
    public async Task AsociaciónDeOtroVehiculo_NoSeConfunde()
    {
        var personaId = Guid.NewGuid();
        var repository = new FakeConductorVehiculoRepository();
        repository.Add(ConductorVehiculo.Crear(personaId, Guid.NewGuid(), Ahora));

        var useCase = CrearUseCase(repository);

        var act = () => useCase.ExecuteAsync(
            new ChangeConductorEstadoInputDto(personaId, Guid.NewGuid(), EstadoConductor.Po),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
