using System.Linq.Expressions;
using ApiPoo2.Application.Exceptions;
using ApiPoo2.Application.IRepositories;
using ApiPoo2.Application.IServices;
using ApiPoo2.Application.UseCases.Bootstrap;
using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Users;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.Tests.Bootstrap;

public sealed class BootstrapAdminUseCaseTests
{
    private static readonly DateTime Ahora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(_ => { });

    [Fact]
    public async Task BaseVacia_CreaAdministrativoYDevuelveCredenciales()
    {
        var personas = new PersonaRepositoryFake();
        var usuarios = new UserRepositoryFake();
        var useCase = CrearUseCase(personas, usuarios);

        var resultado = await useCase.ExecuteAsync(
            new BootstrapAdminInputDto(
                TipoIdentificacion.CedulaCiudadania,
                "1234567890",
                "Juan",
                "Pérez",
                "juan.perez@example.com"),
            CancellationToken.None);

        resultado.Login.Should().Be("JP1234567890");
        resultado.PasswordGenerada.Should().NotBeNullOrWhiteSpace();
        resultado.ApiKey.Should().NotBeNullOrWhiteSpace();
        personas.Items.Should().HaveCount(1);
        personas.Items[0].TipoPersona.Should().Be(TipoPersona.Administrativo);
        personas.Items[0].Usuario.Should().NotBeNull();
        usuarios.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task BaseConDatos_RechazaConBootstrapDisabled()
    {
        var personas = new PersonaRepositoryFake();
        var usuarios = new UserRepositoryFake();
        personas.Add(Persona.Register(
            TipoIdentificacion.CedulaCiudadania,
            "1",
            "Root",
            "Semilla",
            "root@example.com",
            TipoPersona.Administrativo,
            Ahora));
        var useCase = CrearUseCase(personas, usuarios);

        var act = () => useCase.ExecuteAsync(
            new BootstrapAdminInputDto(
                TipoIdentificacion.CedulaCiudadania,
                "999",
                "Otro",
                "Admin",
                "otro@example.com"),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ForbiddenException>();
        exception.Which.Code.Should().Be("bootstrap.disabled");
        personas.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task EmailInvalido_RechazaConErroresDeValidacion()
    {
        var useCase = CrearUseCase(new PersonaRepositoryFake(), new UserRepositoryFake());

        var act = () => useCase.ExecuteAsync(
            new BootstrapAdminInputDto(
                TipoIdentificacion.CedulaCiudadania,
                "123",
                "Juan",
                "Pérez",
                "no-es-email"),
            CancellationToken.None);

        await act.Should().ThrowAsync<RequestValidationException>();
    }

    private static BootstrapAdminUseCase CrearUseCase(
        IPersonaRepository personas,
        IUserRepository usuarios)
        => new(
            [new BootstrapAdminValidator()],
            LoggerFactory,
            personas,
            usuarios,
            new PasswordHasherFake(),
            new DateTimeProviderFake(),
            new UnitOfWorkFake());

    private sealed class PersonaRepositoryFake : IPersonaRepository
    {
        public List<Persona> Items { get; } = [];

        public Task<Persona?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Persona?>(Items.FirstOrDefault(p => p.Id == id));

        public Task<Persona?> GetByNumeroIdentificacionAsync(
            NumeroIdentificacion numero,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Persona?>(Items.FirstOrDefault(p => p.NumeroIdentificacion.Value == numero.Value));

        public Task<Persona?> GetByCorreoAsync(
            CorreoElectronico correo,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Persona?>(Items.FirstOrDefault(p => p.CorreoElectronico.Value == correo.Value));

        public Task<bool> ExistsByNumeroIdentificacionAsync(
            NumeroIdentificacion numero,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Items.Any(p => p.NumeroIdentificacion.Value == numero.Value));

        public Task<bool> ExistsByLoginAsync(Login login, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.Any(p => p.Usuario != null && p.Usuario.Login.Value == login.Value));

        public Task<IReadOnlyDictionary<TipoPersona, int>> CountByTipoAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<TipoPersona, int>>(
                Items.GroupBy(p => p.TipoPersona).ToDictionary(g => g.Key, g => g.Count()));

        public Task<bool> AnyAsync(
            Expression<Func<Persona, bool>> predicate,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Items.AsQueryable().Any(predicate));

        public void Add(Persona entity) => Items.Add(entity);

        public void Update(Persona entity)
        {
        }

        public void Remove(Persona entity) => Items.Remove(entity);
    }

    private sealed class UserRepositoryFake : IUserRepository
    {
        public List<User> Items { get; } = [];

        public Task<User?> GetByPersonaAndLoginAsync(
            Guid idPersona,
            Login login,
            CancellationToken cancellationToken = default)
            => Task.FromResult<User?>(Items.FirstOrDefault(u =>
                u.IdPersona == idPersona && u.Login.Value == login.Value));

        public Task<User?> GetByLoginAsync(Login login, CancellationToken cancellationToken = default)
            => Task.FromResult<User?>(Items.FirstOrDefault(u => u.Login.Value == login.Value));

        public Task<Persona?> GetPersonaByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Persona?>(null);

        public Task<User?> GetByApiKeyAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult<User?>(Items.FirstOrDefault(u => u.ApiKey.Value == apiKey.Value));

        public Task<IReadOnlyList<User>> GetByPersonaAsync(
            Guid idPersona,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<User>>(Items.Where(u => u.IdPersona == idPersona).ToList());

        public void Add(User user) => Items.Add(user);
    }

    private sealed class PasswordHasherFake : IPasswordHasher
    {
        public string Hash(string password) => "HASH:" + password;

        public bool Verify(string password, string passwordHash) => passwordHash == "HASH:" + password;
    }

    private sealed class DateTimeProviderFake : IDateTimeProvider
    {
        public DateTime UtcNow => Ahora;
    }

    private sealed class UnitOfWorkFake : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }
}
