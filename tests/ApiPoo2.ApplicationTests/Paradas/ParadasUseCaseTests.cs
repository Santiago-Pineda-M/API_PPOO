using ApiPoo2.Application.Exceptions;
using ApiPoo2.Application.UseCases.Paradas.Create;
using ApiPoo2.Application.UseCases.Paradas.Delete;
using ApiPoo2.Application.UseCases.Paradas.Get;
using ApiPoo2.Application.UseCases.Paradas.List;
using ApiPoo2.Application.UseCases.Paradas.Update;
using ApiPoo2.ApplicationTests.Common.TestDoubles;
using ApiPoo2.Domain.Rutas;
using FluentAssertions;

namespace ApiPoo2.ApplicationTests.Paradas;

public sealed class ParadasUseCaseTests
{
    private static readonly DateTime Ahora = TestData.Ahora;

    private static (FakeRutaRepository Rutas, Ruta Ruta, FakeUnitOfWork UnitOfWork) CrearEscenario()
    {
        var ruta = DominioDePrueba.RutaOperable("R-001", "Terminal");
        var rutas = new FakeRutaRepository();
        rutas.Add(ruta);

        return (rutas, ruta, new FakeUnitOfWork());
    }

    private static CreateParadaUseCase CrearCreate(
        FakeRutaRepository rutas,
        FakeUnitOfWork unitOfWork)
        => new(
            [new CreateParadaValidator()],
            TestData.LoggerFactory,
            rutas,
            new FakeDateTimeProvider { UtcNow = Ahora },
            unitOfWork);

    [Fact]
    public async Task CreateParada_AgregaLaParadaEnElOrdenIndicado()
    {
        var (rutas, ruta, unitOfWork) = CrearEscenario();

        var resultado = await CrearCreate(rutas, unitOfWork).ExecuteAsync(
            new CreateParadaInputDto("R-001", "Plaza de Bolívar", 2, 4.4389, -75.2322),
            CancellationToken.None);

        resultado.OrdenParada.Should().Be(2);
        resultado.Ubicacion.Should().Be("Plaza de Bolívar");
        resultado.Latitud.Should().Be(4.4389);
        ruta.CantidadParadas().Should().Be(3);
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task CreateParada_RutaInexistente_RechazaConNotFound()
    {
        var (rutas, _, unitOfWork) = CrearEscenario();

        var act = () => CrearCreate(rutas, unitOfWork).ExecuteAsync(
            new CreateParadaInputDto("R-404", "Plaza", 1, null, null),
            CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("ruta.not_found");
    }

    [Fact]
    public async Task CreateParada_OrdenDuplicado_RechazaConConflict()
    {
        var (rutas, _, unitOfWork) = CrearEscenario();

        var act = () => CrearCreate(rutas, unitOfWork).ExecuteAsync(
            new CreateParadaInputDto("R-001", "Plaza", 0, null, null),
            CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>())
            .Which.Code.Should().Be("parada.orden.duplicado");
    }

    [Fact]
    public async Task CreateParada_RutaCompleta_RechazaConConflict()
    {
        var (rutas, ruta, unitOfWork) = CrearEscenario();

        for (var orden = 2; orden <= ReglasParada.OrdenMaximo; orden++)
        {
            ruta.AgregarParada($"Parada {orden}", orden, null, null, Ahora);
        }

        var act = () => CrearCreate(rutas, unitOfWork).ExecuteAsync(
            new CreateParadaInputDto("R-001", "Extra", 1, null, null),
            CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>()).Which.Code.Should().Be("parada.maximo");
    }

    [Fact]
    public async Task CreateParada_OrdenFueraDeRango_FallaLaValidacion()
    {
        var (rutas, _, unitOfWork) = CrearEscenario();

        var act = () => CrearCreate(rutas, unitOfWork).ExecuteAsync(
            new CreateParadaInputDto("R-001", "Plaza", 7, null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<RequestValidationException>();
    }

    [Fact]
    public async Task ListParadas_DevuelveLasParadasOrdenadas()
    {
        var (rutas, ruta, _) = CrearEscenario();
        ruta.AgregarParada("Segunda", 3, null, null, Ahora);
        ruta.AgregarParada("Primera", 2, null, null, Ahora);

        var useCase = new ListParadasUseCase([], TestData.LoggerFactory, rutas);

        var resultado = await useCase.ExecuteAsync(new ListParadasInputDto("R-001"), CancellationToken.None);

        resultado.Select(p => p.OrdenParada).Should().ContainInOrder(0, 1, 2, 3);
    }

    [Fact]
    public async Task ListParadas_RutaInexistente_RechazaConNotFound()
    {
        var (rutas, _, _) = CrearEscenario();
        var useCase = new ListParadasUseCase([], TestData.LoggerFactory, rutas);

        var act = () => useCase.ExecuteAsync(new ListParadasInputDto("R-404"), CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("ruta.not_found");
    }

    [Fact]
    public async Task GetParada_Existente_DevuelveLaParada()
    {
        var (rutas, ruta, _) = CrearEscenario();
        var parada = ruta.Paradas.Single(p => p.EsInicial());

        var useCase = new GetParadaUseCase([], TestData.LoggerFactory, rutas);

        var resultado = await useCase.ExecuteAsync(new GetParadaInputDto(parada.Id), CancellationToken.None);

        resultado.Id.Should().Be(parada.Id);
        resultado.Ubicacion.Should().Be("Terminal");
    }

    [Fact]
    public async Task GetParada_Inexistente_RechazaConNotFound()
    {
        var (rutas, _, _) = CrearEscenario();
        var useCase = new GetParadaUseCase([], TestData.LoggerFactory, rutas);

        var act = () => useCase.ExecuteAsync(new GetParadaInputDto(Guid.NewGuid()), CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("parada.not_found");
    }

    [Fact]
    public async Task UpdateParada_CambiaLosDatosYPersiste()
    {
        var (rutas, ruta, unitOfWork) = CrearEscenario();
        var parada = ruta.AgregarParada("Plaza", 3, null, null, Ahora);
        var useCase = new UpdateParadaUseCase(
            [new UpdateParadaValidator()],
            TestData.LoggerFactory,
            rutas,
            new FakeDateTimeProvider { UtcNow = Ahora },
            unitOfWork);

        var resultado = await useCase.ExecuteAsync(
            new UpdateParadaInputDto(parada.Id, "Mercado", 2, 4.5, -75.2),
            CancellationToken.None);

        resultado.Ubicacion.Should().Be("Mercado");
        resultado.OrdenParada.Should().Be(2);
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task UpdateParada_Inexistente_RechazaConNotFound()
    {
        var (rutas, _, unitOfWork) = CrearEscenario();
        var useCase = new UpdateParadaUseCase(
            [],
            TestData.LoggerFactory,
            rutas,
            new FakeDateTimeProvider(),
            unitOfWork);

        var act = () => useCase.ExecuteAsync(
            new UpdateParadaInputDto(Guid.NewGuid(), "Mercado", 1, null, null),
            CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("parada.not_found");
    }

    [Fact]
    public async Task UpdateParada_OrdenOcupado_RechazaConConflict()
    {
        var (rutas, ruta, unitOfWork) = CrearEscenario();
        var parada = ruta.AgregarParada("Plaza", 2, null, null, Ahora);
        var useCase = new UpdateParadaUseCase(
            [],
            TestData.LoggerFactory,
            rutas,
            new FakeDateTimeProvider(),
            unitOfWork);

        var act = () => useCase.ExecuteAsync(
            new UpdateParadaInputDto(parada.Id, "Mercado", 0, null, null),
            CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>())
            .Which.Code.Should().Be("parada.orden.duplicado");
    }

    [Fact]
    public async Task DeleteParada_EliminaLaParadaNoInicial()
    {
        var (rutas, ruta, unitOfWork) = CrearEscenario();
        var parada = ruta.AgregarParada("Plaza", 2, null, null, Ahora);
        var useCase = new DeleteParadaUseCase(
            [],
            TestData.LoggerFactory,
            rutas,
            new FakeDateTimeProvider { UtcNow = Ahora },
            unitOfWork);

        await useCase.ExecuteAsync(new DeleteParadaInputDto(parada.Id), CancellationToken.None);

        ruta.CantidadParadas().Should().Be(2);
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task DeleteParada_Inicial_RechazaConConflict()
    {
        var (rutas, ruta, unitOfWork) = CrearEscenario();
        var inicial = ruta.Paradas.Single(p => p.EsInicial());
        var useCase = new DeleteParadaUseCase(
            [],
            TestData.LoggerFactory,
            rutas,
            new FakeDateTimeProvider(),
            unitOfWork);

        var act = () => useCase.ExecuteAsync(new DeleteParadaInputDto(inicial.Id), CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>())
            .Which.Code.Should().Be("parada.inicial.requerida");
    }

    [Fact]
    public async Task DeleteParada_Inexistente_RechazaConNotFound()
    {
        var (rutas, _, unitOfWork) = CrearEscenario();
        var useCase = new DeleteParadaUseCase(
            [],
            TestData.LoggerFactory,
            rutas,
            new FakeDateTimeProvider(),
            unitOfWork);

        var act = () => useCase.ExecuteAsync(new DeleteParadaInputDto(Guid.NewGuid()), CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("parada.not_found");
    }
}
