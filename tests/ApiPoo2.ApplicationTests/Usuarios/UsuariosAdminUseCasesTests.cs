using ApiPoo2.Application.Exceptions;
using ApiPoo2.ApplicationTests.Common.TestDoubles;
using ApiPoo2.Application.UseCases.Usuarios.AsignarRol;
using ApiPoo2.Application.UseCases.Usuarios.Crear;
using ApiPoo2.Application.UseCases.Usuarios.Delete;
using ApiPoo2.Application.UseCases.Usuarios.Get;
using ApiPoo2.Application.UseCases.Usuarios.Listar;
using ApiPoo2.Application.UseCases.Usuarios.Update;
using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Users;
using FluentAssertions;

namespace ApiPoo2.ApplicationTests.Usuarios;

public sealed class UsuariosAdminUseCasesTests
{
    private static readonly DateTime Ahora = TestData.Ahora;

    private static Persona CrearPersonaAdministrativaConUsuario(string loginSufijo = "")
    {
        var numero = "123456789" + loginSufijo;
        var persona = Persona.Register(
            TipoIdentificacion.CedulaCiudadania,
            numero,
            "Juan",
            "Pérez",
            $"juan{loginSufijo}perez@example.com",
            TipoPersona.Administrativo,
            Ahora);

        var hash = new FakePasswordHasher().Hash("Pass123!");
        persona.CrearUsuario(hash, Ahora);

        return persona;
    }

    private static (ListarUsuariosUseCase UseCase, FakePersonaRepository Personas) ListarUseCaseConDatos(
        params Persona[] personas)
    {
        var personasRepo = new FakePersonaRepository();
        foreach (var p in personas)
        {
            personasRepo.Items.Add(p);
        }

        var useCase = new ListarUsuariosUseCase(
            [],
            TestData.LoggerFactory,
            personasRepo);

        return (useCase, personasRepo);
    }

    [Fact]
    public async Task ListarUsuarios_DevuelveSoloPersonasConUsuarioYPaginado()
    {
        var p1 = CrearPersonaAdministrativaConUsuario("1");
        var p2 = CrearPersonaAdministrativaConUsuario("2");
        var p3 = Persona.Register(
            TipoIdentificacion.CedulaCiudadania,
            "999999999",
            "Sin",
            "Usuario",
            "sin.usuario@example.com",
            TipoPersona.Conductor,
            Ahora); // sin usuario: no debe aparecer

        var (useCase, _) = ListarUseCaseConDatos(p1, p2, p3);

        var resultado = await useCase.ExecuteAsync(new ListarUsuariosInputDto(1, 10), CancellationToken.None);

        resultado.Items.Should().HaveCount(2);
        resultado.Total.Should().Be(2);
        resultado.Items.Select(i => i.Login).Should().Contain(p1.Usuario!.Login.Value, p2.Usuario!.Login.Value);
    }

    [Fact]
    public async Task ListarUsuarios_Pagina2_DevuelveVacio()
    {
        var p1 = CrearPersonaAdministrativaConUsuario("1");

        var (useCase, _) = ListarUseCaseConDatos(p1);

        var resultado = await useCase.ExecuteAsync(new ListarUsuariosInputDto(2, 10), CancellationToken.None);

        resultado.Items.Should().BeEmpty();
        resultado.Total.Should().Be(1);
    }

    [Fact]
    public async Task GetUsuario_DevuelveDetalleDelUsuario()
    {
        var persona = CrearPersonaAdministrativaConUsuario();
        var personasRepo = new FakePersonaRepository { Items = { persona } };

        var useCase = new GetUsuarioUseCase(
            [],
            TestData.LoggerFactory,
            personasRepo);

        var resultado = await useCase.ExecuteAsync(new GetUsuarioInputDto(persona.Id), CancellationToken.None);

        resultado.Id.Should().Be(persona.Id);
        resultado.Login.Should().Be(persona.Usuario!.Login.Value);
        resultado.Rol.Should().Be(persona.Usuario.Role.ToString());
        resultado.IsActive.Should().BeTrue();
        resultado.Nombres.Should().Be("Juan");
    }

    [Fact]
    public async Task GetUsuario_PersonaInexistente_NotFound()
    {
        var useCase = new GetUsuarioUseCase(
            [],
            TestData.LoggerFactory,
            new FakePersonaRepository());

        var act = () => useCase.ExecuteAsync(new GetUsuarioInputDto(Guid.NewGuid()), CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("persona.not_found");
    }

    [Fact]
    public async Task GetUsuario_PersonaSinUsuario_NotFound()
    {
        var persona = Persona.Register(
            TipoIdentificacion.CedulaCiudadania,
            "111111111",
            "Ana",
            "Gómez",
            "ana.gomez@example.com",
            TipoPersona.Conductor,
            Ahora);

        var personasRepo = new FakePersonaRepository { Items = { persona } };

        var useCase = new GetUsuarioUseCase(
            [],
            TestData.LoggerFactory,
            personasRepo);

        var act = () => useCase.ExecuteAsync(new GetUsuarioInputDto(persona.Id), CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("usuario.no_encontrado");
    }

    [Fact]
    public async Task CrearUsuario_GeneraLoginMnemonicoYPersiste()
    {
        var persona = Persona.Register(
            TipoIdentificacion.CedulaCiudadania,
            "123456789",
            "Maria",
            "López",
            "maria.lopez@example.com",
            TipoPersona.Administrativo,
            Ahora);

        var personasRepo = new FakePersonaRepository { Items = { persona } };
        var usuariosRepo = new FakeUserRepository();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new CrearUsuarioUseCase(
            [],
            TestData.LoggerFactory,
            personasRepo,
            usuariosRepo,
            new FakePasswordHasher(),
            new FakeDateTimeProvider { UtcNow = Ahora },
            unitOfWork);

        var resultado = await useCase.ExecuteAsync(
            new CrearUsuarioInputDto(persona.Id, "Pass123!"),
            CancellationToken.None);

        resultado.Id.Should().Be(persona.Id);
        resultado.Login.Should().StartWith("ML");
        resultado.Rol.Should().Be(UserRole.Administrator.ToString());
        resultado.IsActive.Should().BeTrue();
        usuariosRepo.Items.Should().ContainSingle();
        unitOfWork.SaveChangesCalls.Should().Be(1);
        persona.Usuario.Should().NotBeNull();
        persona.Usuario!.ApiKey.Value.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CrearUsuario_PersonaInexistente_NotFound()
    {
        var useCase = new CrearUsuarioUseCase(
            [],
            TestData.LoggerFactory,
            new FakePersonaRepository(),
            new FakeUserRepository(),
            new FakePasswordHasher(),
            new FakeDateTimeProvider(),
            new FakeUnitOfWork());

        var act = () => useCase.ExecuteAsync(
            new CrearUsuarioInputDto(Guid.NewGuid(), "Pass123!"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("persona.not_found");
    }

    [Fact]
    public async Task CrearUsuario_PersonaSinPermiso_DominioRechaza()
    {
        var persona = Persona.Register(
            TipoIdentificacion.CedulaCiudadania,
            "222222222",
            "Carlos",
            "Ruiz",
            "carlos.ruiz@example.com",
            TipoPersona.Conductor,
            Ahora);

        var personasRepo = new FakePersonaRepository { Items = { persona } };

        var useCase = new CrearUsuarioUseCase(
            [],
            TestData.LoggerFactory,
            personasRepo,
            new FakeUserRepository(),
            new FakePasswordHasher(),
            new FakeDateTimeProvider(),
            new FakeUnitOfWork());

        var act = () => useCase.ExecuteAsync(
            new CrearUsuarioInputDto(persona.Id, "Pass123!"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<DomainValidationException>())
            .Which.Code.Should().Be("persona.usuario.no_admitido");
    }

    [Fact]
    public async Task CrearUsuario_PersonaConUsuarioYa_DominioRechaza()
    {
        var persona = CrearPersonaAdministrativaConUsuario();
        var personasRepo = new FakePersonaRepository { Items = { persona } };

        var useCase = new CrearUsuarioUseCase(
            [],
            TestData.LoggerFactory,
            personasRepo,
            new FakeUserRepository(),
            new FakePasswordHasher(),
            new FakeDateTimeProvider(),
            new FakeUnitOfWork());

        var act = () => useCase.ExecuteAsync(
            new CrearUsuarioInputDto(persona.Id, "Pass123!"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<DomainValidationException>())
            .Which.Code.Should().Be("persona.usuario.duplicado");
    }

    [Fact]
    public async Task CrearUsuario_ContraseniaInvalida_DominioRechaza()
    {
        var persona = Persona.Register(
            TipoIdentificacion.CedulaCiudadania,
            "333333333",
            "Lucía",
            "Díaz",
            "lucia.diaz@example.com",
            TipoPersona.Administrativo,
            Ahora);

        var personasRepo = new FakePersonaRepository { Items = { persona } };

        var useCase = new CrearUsuarioUseCase(
            [],
            TestData.LoggerFactory,
            personasRepo,
            new FakeUserRepository(),
            new FakePasswordHasher(),
            new FakeDateTimeProvider(),
            new FakeUnitOfWork());

        var act = () => useCase.ExecuteAsync(
            new CrearUsuarioInputDto(persona.Id, "debil"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<DomainValidationException>())
            .Which.Code.Should().Be("password.policy");
    }

    [Fact]
    public async Task UpdateUsuario_EditaDatosYEstado()
    {
        var persona = CrearPersonaAdministrativaConUsuario();
        var personasRepo = new FakePersonaRepository { Items = { persona } };
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new UpdateUsuarioUseCase(
            [],
            TestData.LoggerFactory,
            personasRepo,
            new FakeDateTimeProvider { UtcNow = Ahora },
            unitOfWork);

        var resultado = await useCase.ExecuteAsync(
            new UpdateUsuarioInputDto(
                persona.Id,
                "María",
                "López",
                "maria.lopez@example.com",
                TipoPersona.Administrativo,
                false),
            CancellationToken.None);

        resultado.Nombres.Should().Be("María");
        resultado.CorreoElectronico.Should().Be("maria.lopez@example.com");
        resultado.IsActive.Should().BeFalse();
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task UpdateUsuario_CorreoDuplicado_Conflicto()
    {
        var objetivo = CrearPersonaAdministrativaConUsuario();
        var otro = CrearPersonaAdministrativaConUsuario("9");

        var personasRepo = new FakePersonaRepository { Items = { objetivo, otro } };
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new UpdateUsuarioUseCase(
            [],
            TestData.LoggerFactory,
            personasRepo,
            new FakeDateTimeProvider { UtcNow = Ahora },
            unitOfWork);

        var act = () => useCase.ExecuteAsync(
            new UpdateUsuarioInputDto(
                objetivo.Id,
                null,
                null,
                $"juan{9}perez@example.com", // correo de 'otro'
                null,
                null),
            CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>())
            .Which.Code.Should().Be("persona.correo.conflict");
    }

    [Fact]
    public async Task DeleteUsuario_DesactivaAlUsuario()
    {
        var persona = CrearPersonaAdministrativaConUsuario();
        var personasRepo = new FakePersonaRepository { Items = { persona } };
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new DeleteUsuarioUseCase(
            [],
            TestData.LoggerFactory,
            personasRepo,
            new FakeDateTimeProvider { UtcNow = Ahora },
            unitOfWork);

        var resultado = await useCase.ExecuteAsync(
            new DeleteUsuarioInputDto(persona.Id),
            CancellationToken.None);

        resultado.Succeeded.Should().BeTrue();
        persona.Usuario!.IsActive.Should().BeFalse();
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task DeleteUsuario_Inexistente_NotFound()
    {
        var useCase = new DeleteUsuarioUseCase(
            [],
            TestData.LoggerFactory,
            new FakePersonaRepository(),
            new FakeDateTimeProvider(),
            new FakeUnitOfWork());

        var act = () => useCase.ExecuteAsync(new DeleteUsuarioInputDto(Guid.NewGuid()), CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>())
            .Which.Code.Should().Be("persona.not_found");
    }

    [Fact]
    public async Task AsignarRol_CambiaRolYPersiste()
    {
        var persona = CrearPersonaAdministrativaConUsuario(); // rol Administrator por defecto
        var personasRepo = new FakePersonaRepository { Items = { persona } };
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new AsignarRolUseCase(
            [],
            TestData.LoggerFactory,
            personasRepo,
            new FakeDateTimeProvider { UtcNow = Ahora },
            unitOfWork);

        var resultado = await useCase.ExecuteAsync(
            new AsignarRolInputDto(persona.Id, UserRole.Member),
            CancellationToken.None);

        resultado.Rol.Should().Be(UserRole.Member.ToString());
        persona.Usuario!.Role.Should().Be(UserRole.Member);
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task AsignarRol_UsuarioInexistente_NotFound()
    {
        var persona = Persona.Register(
            TipoIdentificacion.CedulaCiudadania,
            "444444444",
            "Sofía",
            "Mora",
            "sofia.mora@example.com",
            TipoPersona.Conductor,
            Ahora);

        var personasRepo = new FakePersonaRepository { Items = { persona } };

        var useCase = new AsignarRolUseCase(
            [],
            TestData.LoggerFactory,
            personasRepo,
            new FakeDateTimeProvider(),
            new FakeUnitOfWork());

        var act = () => useCase.ExecuteAsync(
            new AsignarRolInputDto(persona.Id, UserRole.Member),
            CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>())
            .Which.Code.Should().Be("usuario.no_encontrado");
    }
}
