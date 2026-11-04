using System.Security.Claims;
using ApiPoo2.Application.IRepositories;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Users;
using ApiPoo2.WebApi.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPoo2.WebApiTests.Auth;

public sealed class ApiKeyHandlerTests
{
    private static ClaimsPrincipal ConstruirPrincipal(Guid personaId)
        => new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, personaId.ToString())],
            "Bearer"));

    private static AuthorizationHandlerContext CrearContexto(ClaimsPrincipal principal, HttpContext httpContext)
        => new(
            [new ApiKeyRequirement()],
            principal,
            httpContext);

    private static HttpContext HttpConApiKey(string? valorHeader)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestAborted = CancellationToken.None;

        if (valorHeader is not null)
        {
            httpContext.Request.Headers[ApiKeyRequirement.HeaderName] = valorHeader;
        }

        return httpContext;
    }

    private static (Persona Persona, User Usuario) CrearUsuario(string identificacion)
    {
        var persona = Persona.Register(
            TipoIdentificacion.CedulaCiudadania,
            identificacion,
            "Ana",
            "Gómez",
            $"ana-{identificacion}@example.com",
            TipoPersona.Administrativo,
            DateTime.UtcNow);

        var usuario = persona.CrearUsuario("HASH:Password123!", DateTime.UtcNow);
        return (persona, usuario);
    }

    private async Task Manejar(AuthorizationHandlerContext context, User? usuario)
    {
        var handler = new ApiKeyHandler(new StubScopeFactory(usuario));
        await handler.HandleAsync(context);
    }

    [Fact]
    public async Task UsuarioNoAutenticado_NoNiegaNiAprueba()
    {
        var (persona, usuario) = CrearUsuario("1001");
        var context = CrearContexto(new ClaimsPrincipal(), HttpConApiKey(usuario.ApiKey.Value));

        await Manejar(context, usuario);

        context.HasFailed.Should().BeFalse();
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task SinHttpContext_NoNiegaNiAprueba()
    {
        var (persona, _) = CrearUsuario("1002");
        var context = new AuthorizationHandlerContext(
            [new ApiKeyRequirement()], ConstruirPrincipal(persona.Id), null);

        await Manejar(context, null);

        context.HasFailed.Should().BeFalse();
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HeaderAusente_FallaLaAutorizacion()
    {
        var (persona, _) = CrearUsuario("1003");
        var context = CrearContexto(ConstruirPrincipal(persona.Id), HttpConApiKey(null));

        await Manejar(context, null);

        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task HeaderVacio_FallaLaAutorizacion()
    {
        var (persona, _) = CrearUsuario("1004");
        var context = CrearContexto(ConstruirPrincipal(persona.Id), HttpConApiKey("   "));

        await Manejar(context, null);

        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task ApiKeyInvalida_FallaLaAutorizacion()
    {
        var (persona, _) = CrearUsuario("1005");
        var context = CrearContexto(ConstruirPrincipal(persona.Id), HttpConApiKey("!!!"));

        await Manejar(context, CrearUsuario("9999").Usuario);

        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task ApiKeyDesconocida_FallaLaAutorizacion()
    {
        var (persona, _) = CrearUsuario("1006");
        var context = CrearContexto(ConstruirPrincipal(persona.Id), HttpConApiKey(ApiKey.Generate().Value));

        await Manejar(context, null);

        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task ApiKeyDeOtroUsuario_FallaLaAutorizacion()
    {
        var (personaA, usuarioA) = CrearUsuario("1007");
        var (personaB, _) = CrearUsuario("2007");
        var context = CrearContexto(ConstruirPrincipal(personaB.Id), HttpConApiKey(usuarioA.ApiKey.Value));

        await Manejar(context, usuarioA);

        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task ApiKeyValidaYPropia_ApruebaLaAutorizacion()
    {
        var (persona, usuario) = CrearUsuario("1008");
        var context = CrearContexto(ConstruirPrincipal(persona.Id), HttpConApiKey(usuario.ApiKey.Value));

        await Manejar(context, usuario);

        context.HasFailed.Should().BeFalse();
        context.HasSucceeded.Should().BeTrue();
    }

    private sealed class StubScopeFactory : IServiceScopeFactory
    {
        private readonly User? _usuario;

        public StubScopeFactory(User? usuario) => _usuario = usuario;

        public IServiceScope CreateScope() => new StubScope(_usuario);

        private sealed class StubScope : IServiceScope
        {
            public IServiceProvider ServiceProvider { get; }

            public StubScope(User? usuario) => ServiceProvider = new StubProvider(usuario);

            public void Dispose()
            {
            }
        }

        private sealed class StubProvider(User? usuario) : IServiceProvider
        {
            public object? GetService(Type serviceType)
                => serviceType == typeof(IUserRepository) ? new UsuarioFake(usuario) : null;
        }
    }

    private sealed class UsuarioFake(User? usuario) : IUserRepository
    {
        public Task<User?> GetByApiKeyAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult(usuario is not null && usuario.ApiKey.Value == apiKey.Value ? usuario : null);

        public Task<User?> GetByLoginAsync(Login login, CancellationToken cancellationToken = default)
            => Task.FromResult<User?>(null);

        public Task<User?> GetByPersonaAndLoginAsync(Guid idPersona, Login login, CancellationToken cancellationToken = default)
            => Task.FromResult<User?>(null);

        public Task<Persona?> GetPersonaByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Persona?>(null);

        public Task<IReadOnlyList<User>> GetByPersonaAsync(Guid idPersona, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<User>>([]);

        public void Add(User user)
        {
        }
    }
}
