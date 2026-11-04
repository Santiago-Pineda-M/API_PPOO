using ApiPoo2.Application.Exceptions;
using ApiPoo2.Application.UseCases.Personas.Licencia;
using ApiPoo2.ApplicationTests.Common.TestDoubles;
using ApiPoo2.Domain.Personas;
using FluentAssertions;

namespace ApiPoo2.ApplicationTests.Personas;

public sealed class LicenciaConductorUseCasesTests
{
    private static readonly DateTime Ahora = TestData.Ahora;

    private static (FakePersonaRepository Personas, Persona Conductor, FakeUnitOfWork UnitOfWork)
        CrearEscenario(bool conLicencia = true)
    {
        var persona = DominioDePrueba.ConductorConLicencia();
        var personas = new FakePersonaRepository();

        if (!conLicencia)
        {
            persona.EliminarLicencia(Ahora);
        }

        personas.Add(persona);
        return (personas, persona, new FakeUnitOfWork());
    }

    private static GetLicenciaConductorUseCase CrearGet(FakePersonaRepository personas)
        => new(
            [],
            TestData.LoggerFactory,
            personas,
            new FakeDateTimeProvider { UtcNow = Ahora });

    [Fact]
    public async Task GetLicencia_ConLicenciaRegistrada_DevuelveMetadatosSinElBinario()
    {
        var (personas, conductor, _) = CrearEscenario();

        var resultado = await CrearGet(personas).ExecuteAsync(
            new GetLicenciaConductorInputDto(conductor.Id),
            CancellationToken.None);

        resultado.PersonaId.Should().Be(conductor.Id);
        resultado.Vigente.Should().BeTrue();
        resultado.FechaVigencia.Should().Be(conductor.FechaVigenciaLicencia);
        resultado.LongitudBytes.Should().Be(TestData.PdfValido.Length);
    }

    [Fact]
    public async Task GetLicencia_SinLicencia_RechazaConNotFound()
    {
        var (personas, conductor, _) = CrearEscenario(conLicencia: false);

        var act = () => CrearGet(personas).ExecuteAsync(
            new GetLicenciaConductorInputDto(conductor.Id),
            CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>())
            .Which.Code.Should().Be("persona.licencia.not_found");
    }

    [Fact]
    public async Task GetLicencia_PersonaInexistente_RechazaConNotFound()
    {
        var (personas, _, _) = CrearEscenario();

        var act = () => CrearGet(personas).ExecuteAsync(
            new GetLicenciaConductorInputDto(Guid.NewGuid()),
            CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("person.not_found");
    }

    [Fact]
    public async Task RegistrarLicencia_ConductorValido_PersisteElPdf()
    {
        var (personas, conductor, unitOfWork) = CrearEscenario(conLicencia: false);
        var useCase = new RegistrarLicenciaConductorUseCase(
            [new RegistrarLicenciaConductorValidator()],
            TestData.LoggerFactory,
            personas,
            new FakeDateTimeProvider { UtcNow = Ahora },
            unitOfWork);

        var resultado = await useCase.ExecuteAsync(
            new RegistrarLicenciaConductorInputDto(
                conductor.Id,
                Convert.ToBase64String(TestData.PdfValido),
                Ahora.AddMonths(6)),
            CancellationToken.None);

        resultado.Vigente.Should().BeTrue();
        conductor.TieneLicencia().Should().BeTrue();
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task RegistrarLicencia_PersonaNoConductora_RechazaConForbidden()
    {
        var administrativo = Persona.Register(
            TipoIdentificacion.CedulaCiudadania,
            "9999999999",
            "Ana",
            "Gómez",
            "ana@example.com",
            TipoPersona.Administrativo,
            Ahora);
        var personas = new FakePersonaRepository();
        personas.Add(administrativo);

        var useCase = new RegistrarLicenciaConductorUseCase(
            [],
            TestData.LoggerFactory,
            personas,
            new FakeDateTimeProvider(),
            new FakeUnitOfWork());

        var act = () => useCase.ExecuteAsync(
            new RegistrarLicenciaConductorInputDto(
                administrativo.Id,
                Convert.ToBase64String(TestData.PdfValido),
                Ahora.AddMonths(1)),
            CancellationToken.None);

        (await act.Should().ThrowAsync<ForbiddenException>())
            .Which.Code.Should().Be("persona.licencia.tipo_persona");
    }

    [Fact]
    public async Task RegistrarLicencia_Base64Invalido_RechazaConRequestValidation()
    {
        var (personas, conductor, unitOfWork) = CrearEscenario(conLicencia: false);
        var useCase = new RegistrarLicenciaConductorUseCase(
            [],
            TestData.LoggerFactory,
            personas,
            new FakeDateTimeProvider(),
            unitOfWork);

        var act = () => useCase.ExecuteAsync(
            new RegistrarLicenciaConductorInputDto(
                conductor.Id,
                "no-es-base64",
                Ahora.AddMonths(1)),
            CancellationToken.None);

        await act.Should().ThrowAsync<RequestValidationException>();
    }

    [Fact]
    public async Task RegistrarLicencia_PersonaInexistente_RechazaConNotFound()
    {
        var (personas, _, unitOfWork) = CrearEscenario();
        var useCase = new RegistrarLicenciaConductorUseCase(
            [],
            TestData.LoggerFactory,
            personas,
            new FakeDateTimeProvider(),
            unitOfWork);

        var act = () => useCase.ExecuteAsync(
            new RegistrarLicenciaConductorInputDto(
                Guid.NewGuid(),
                Convert.ToBase64String(TestData.PdfValido),
                Ahora.AddMonths(1)),
            CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("person.not_found");
    }

    [Fact]
    public async Task EliminarLicencia_QuitaLaLicenciaYPersiste()
    {
        var (personas, conductor, unitOfWork) = CrearEscenario();
        var useCase = new EliminarLicenciaConductorUseCase(
            [],
            TestData.LoggerFactory,
            personas,
            new FakeDateTimeProvider { UtcNow = Ahora },
            unitOfWork);

        await useCase.ExecuteAsync(
            new EliminarLicenciaConductorInputDto(conductor.Id),
            CancellationToken.None);

        conductor.TieneLicencia().Should().BeFalse();
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task EliminarLicencia_PersonaInexistente_RechazaConNotFound()
    {
        var (personas, _, unitOfWork) = CrearEscenario();
        var useCase = new EliminarLicenciaConductorUseCase(
            [],
            TestData.LoggerFactory,
            personas,
            new FakeDateTimeProvider(),
            unitOfWork);

        var act = () => useCase.ExecuteAsync(
            new EliminarLicenciaConductorInputDto(Guid.NewGuid()),
            CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("person.not_found");
    }
}
