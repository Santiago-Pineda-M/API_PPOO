using ApiPoo2.Application.Exceptions;
using ApiPoo2.ApplicationTests.Common.TestDoubles;
using ApiPoo2.Application.UseCases.Auth;
using ApiPoo2.Application.UseCases.Usuarios;
using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Users;
using FluentAssertions;

namespace ApiPoo2.ApplicationTests.Usuarios;

public sealed class UsuariosUseCasesTests
{
    private static readonly DateTime Ahora = TestData.Ahora;

    private static Persona CrearAdministrativo()
        => Persona.Register(
            TipoIdentificacion.CedulaCiudadania, "1234567890", "Juan", "Pérez",
            "juan@example.com", TipoPersona.Administrativo, Ahora);

    private static (Persona Persona, User Usuario) CrearAdministrativoConUsuario()
    {
        var persona = CrearAdministrativo();
        var hash = new FakePasswordHasher().Hash("Admin123!");
        var usuario = persona.CrearUsuario(hash, Ahora);
        return (persona, usuario);
    }

    [Fact]
    public async Task RegenerarApiKey_GeneraUnaClaveDistintaYPersiste()
    {
        var (persona, usuario) = CrearAdministrativoConUsuario();
        var apiKeyOriginal = usuario.ApiKey.Value;

        var repository = new FakeUserRepository();
        repository.Add(usuario);
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new RegenerateApiKeyUseCase(
            [],
            TestData.LoggerFactory,
            repository,
            new FakeDateTimeProvider { UtcNow = Ahora },
            unitOfWork);

        var resultado = await useCase.ExecuteAsync(
            new RegenerateApiKeyInputDto(usuario.Login.Value),
            CancellationToken.None);

        resultado.Login.Should().Be(usuario.Login.Value);
        resultado.ApiKey.Should().NotBe(apiKeyOriginal);
        resultado.ApiKey.Should().NotBeNullOrWhiteSpace();
        repository.Items.Should().ContainSingle();
        unitOfWork.SaveChangesCalls.Should().Be(1);
        persona.Usuario!.ApiKey.Value.Should().Be(resultado.ApiKey);
    }

    [Fact]
    public async Task RegenerarApiKey_UsuarioInexistente_RechazaConUserNotFound()
    {
        var useCase = new RegenerateApiKeyUseCase(
            [],
            TestData.LoggerFactory,
            new FakeUserRepository(),
            new FakeDateTimeProvider(),
            new FakeUnitOfWork());

        var act = () => useCase.ExecuteAsync(
            new RegenerateApiKeyInputDto("JP999"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("user.not_found");
    }

    [Fact]
    public async Task RegenerarApiKey_LoginInvalido_RechazaPorDominio()
    {
        var useCase = new RegenerateApiKeyUseCase(
            [],
            TestData.LoggerFactory,
            new FakeUserRepository(),
            new FakeDateTimeProvider(),
            new FakeUnitOfWork());

        var act = () => useCase.ExecuteAsync(
            new RegenerateApiKeyInputDto("con espacio"),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<DomainValidationException>();
        exception.Which.Code.Should().Be("user.login");
    }

    [Fact]
    public async Task ChangePassword_CambiaElHashYPersiste()
    {
        var (_, usuario) = CrearAdministrativoConUsuario();
        var repository = new FakeUserRepository();
        repository.Add(usuario);
        var hasher = new FakePasswordHasher();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new ChangeUserPasswordUseCase(
            [],
            TestData.LoggerFactory,
            repository,
            hasher,
            new FakeDateTimeProvider { UtcNow = Ahora },
            unitOfWork);

        var resultado = await useCase.ExecuteAsync(
            new ChangeUserPasswordInputDto(usuario.Login.Value, "Nueva123!"),
            CancellationToken.None);

        resultado.Succeeded.Should().BeTrue();
        hasher.HashCalls.Should().Be(1);
        hasher.Verify("Nueva123!", usuario.PasswordHash.Hash).Should().BeTrue();
        unitOfWork.SaveChangesCalls.Should().Be(1);
        usuario.UpdatedAtUtc.Should().Be(Ahora);
    }

    [Fact]
    public async Task ChangePassword_UsuarioInexistente_RechazaConUserNotFound()
    {
        var useCase = new ChangeUserPasswordUseCase(
            [],
            TestData.LoggerFactory,
            new FakeUserRepository(),
            new FakePasswordHasher(),
            new FakeDateTimeProvider(),
            new FakeUnitOfWork());

        var act = () => useCase.ExecuteAsync(
            new ChangeUserPasswordInputDto("JP999", "Nueva123!"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("user.not_found");
    }

    [Fact]
    public async Task ChangePassword_LoginInvalido_RechazaPorDominio()
    {
        var useCase = new ChangeUserPasswordUseCase(
            [],
            TestData.LoggerFactory,
            new FakeUserRepository(),
            new FakePasswordHasher(),
            new FakeDateTimeProvider(),
            new FakeUnitOfWork());

        var act = () => useCase.ExecuteAsync(
            new ChangeUserPasswordInputDto("con espacio", "Nueva123!"),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<DomainValidationException>();
        exception.Which.Code.Should().Be("user.login");
    }
}
