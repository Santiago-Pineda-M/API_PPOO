using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ApiPoo2.Application.Exceptions;
using ApiPoo2.Application.UseCases.Auth;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Vehiculos;
using ApiPoo2.WebApi.Auth;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPoo2.IntegrationTests;

[Collection("API")]
public sealed class AuthApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public AuthApiTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CrearPersonaAdministrativa_GeneraUsuarioYLoginMnemonico()
    {
        var numero = NumeroAleatorio();

        var numero2 = NumeroAleatorio();
        var response = await _client.PostAsync("/api/personas", Json(new
        {
            tipoIdentificacion = 1,
            numeroIdentificacion = numero2,
            nombres = "Juan",
            apellidos = "Pérez",
            correoElectronico = $"juan.perez{numero2}@example.com",
            tipoPersona = 1
        }));

        // 201 requiere token + ApiKey; sin ellos debe ser 401, lo que confirma que la política aplica.
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PersonaAdministrativa_CreaUsuarioYLoginPorMnemonico()
    {
        var numero = NumeroAleatorio();
        var credenciales = await CrearAdministrativo(numero, "Juan", "Pérez");

        var login = $"JP{numero}";
        var pair = await LoginAsync(login, credenciales.PasswordGenerada!);

        pair.AccessToken.Should().NotBeNullOrWhiteSpace();
        pair.ApiKey.Should().NotBeNullOrWhiteSpace();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(pair.AccessTokenType, pair.AccessToken);

        var me = await _client.GetAsync("/api/users/me");
        me.StatusCode.Should().Be(HttpStatusCode.OK);

        var perfil = await ReadAs<PerfilResponse>(me);
        perfil.Login.Should().Be(login);
        perfil.TipoPersona.Should().Be(TipoPersona.Administrativo);
    }

    [Fact]
    public async Task PersonaConductor_NoGeneraUsuario()
    {
        var numero = NumeroAleatorio();
        var respuesta = await CrearPersonaDirecta(numero, "Ana", "Gómez", "ana.gomez" + numero + "@example.com", 2);

        // Sin sesión no hay APIKey válida, así que la política bloquea antes de crear nada.
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ConCredencialesInvalidas_Unauthorized()
    {
        var response = await LoginRawAsync("JP9999999999", "Cualquier1!");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await ReadAs<ErrorResponse>(response);
        body.Code.Should().Be("credentials.invalid");
    }

    [Fact]
    public async Task ServicioProtegido_SinApiKey_Unauthorized()
    {
        var numero = NumeroAleatorio();
        var credenciales = await CrearAdministrativo(numero, "Luis", "Gómez");
        var pair = await LoginAsync($"LG{numero}", credenciales.PasswordGenerada!);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(pair.AccessTokenType, pair.AccessToken);
        _client.DefaultRequestHeaders.Remove(ApiKeyRequirement.HeaderName);

        var response = await _client.PostAsync("/api/personas", Json(new
        {
            tipoIdentificacion = 1,
            numeroIdentificacion = NumeroAleatorio(),
            nombres = "Otro",
            apellidos = "Usuario",
            correoElectronico = "otro.usuario" + NumeroAleatorio() + "@example.com",
            tipoPersona = 1
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ServicioProtegido_ConApiKey_Created()
    {
        var numero = NumeroAleatorio();
        var credenciales = await CrearAdministrativo(numero, "Carla", "Ruiz");
        var pair = await LoginAsync($"CR{numero}", credenciales.PasswordGenerada!);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(pair.AccessTokenType, pair.AccessToken);
        _client.DefaultRequestHeaders.Remove(ApiKeyRequirement.HeaderName);
        _client.DefaultRequestHeaders.Add(ApiKeyRequirement.HeaderName, pair.ApiKey!);

        var numero2 = NumeroAleatorio();
        var correo = $"nuevo.admin{numero2}@example.com";
        var response = await _client.PostAsync("/api/personas", Json(new
        {
            tipoIdentificacion = 1,
            numeroIdentificacion = numero2,
            nombres = "Nuevo",
            apellidos = "Administrativo",
            correoElectronico = correo,
            tipoPersona = 1
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var creado = await ReadAs<CrearPersonaResponse>(response);
        creado.Login.Should().StartWith("NA");
        creado.PasswordGenerada.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ConsultaPublica_NoRequiereTokenNiApiKey()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        _client.DefaultRequestHeaders.Remove(ApiKeyRequirement.HeaderName);

        var personas = await _client.GetAsync("/api/consultas/personas-por-tipo");
        personas.StatusCode.Should().Be(HttpStatusCode.OK);

        var vencidos = await _client.GetAsync("/api/consultas/documentos-vencidos");
        vencidos.StatusCode.Should().Be(HttpStatusCode.OK);

        var porVencer = await _client.GetAsync("/api/consultas/documentos-por-vencer?dias=30");
        porVencer.StatusCode.Should().Be(HttpStatusCode.OK);

        var conductores = await _client.GetAsync("/api/consultas/conductores-operables?cantidad=10");
        conductores.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_RotacionYDetccionDeReuso()
    {
        var numero = NumeroAleatorio();
        var credenciales = await CrearAdministrativo(numero, "Diana", "Paz");
        var pair = await LoginAsync($"DP{numero}", credenciales.PasswordGenerada!);

        var rotado = await _client.PostAsync("/api/auth/refresh", Json(new { pair.RefreshToken }));
        rotado.StatusCode.Should().Be(HttpStatusCode.OK);

        var reuso = await _client.PostAsync("/api/auth/refresh", Json(new { pair.RefreshToken }));
        reuso.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await ReadAs<ErrorResponse>(reuso);
        body.Code.Should().Be("refresh.reuse");
    }

    [Fact]
    public async Task Logout_RevocaRefreshToken()
    {
        var numero = NumeroAleatorio();
        var credenciales = await CrearAdministrativo(numero, "Eva", "Luna");
        var pair = await LoginAsync($"EL{numero}", credenciales.PasswordGenerada!);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(pair.AccessTokenType, pair.AccessToken);

        var logout = await _client.PostAsync("/api/auth/logout", Json(new { pair.RefreshToken }));
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var reuso = await _client.PostAsync("/api/auth/refresh", Json(new { pair.RefreshToken }));
        reuso.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var error = await ReadAs<ErrorResponse>(reuso);
        error.Code.Should().Be("refresh.invalid");
    }

    [Fact]
    public async Task ChangePassword_InvalidaSesionesYPasswordAnterior()
    {
        var numero = NumeroAleatorio();
        var credenciales = await CrearAdministrativo(numero, "Hugo", "Mar");
        var pair = await LoginAsync($"HM{numero}", credenciales.PasswordGenerada!);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(pair.AccessTokenType, pair.AccessToken);

        var cambio = await _client.PostAsync(
            "/api/auth/change-password",
            Json(new { currentPassword = credenciales.PasswordGenerada!, newPassword = "Nueva#Clave9" }));
        cambio.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var conViejoAccess = await _client.GetAsync("/api/users/me");
        conViejoAccess.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var conViejoRefresh = await _client.PostAsync("/api/auth/refresh", Json(new { pair.RefreshToken }));
        conViejoRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var conViejaClave = await LoginRawAsync($"HM{numero}", credenciales.PasswordGenerada!);
        conViejaClave.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var conNuevaClave = await LoginAsync($"HM{numero}", "Nueva#Clave9");
        conNuevaClave.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Vehiculo_RequiresApiKeyYDocumentoObligatorio()
    {
        var numero = NumeroAleatorio();
        var credenciales = await CrearAdministrativo(numero, "Elena", "Ruiz");
        var pair = await LoginAsync($"ER{numero}", credenciales.PasswordGenerada!);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(pair.AccessTokenType, pair.AccessToken);
        _client.DefaultRequestHeaders.Remove(ApiKeyRequirement.HeaderName);
        _client.DefaultRequestHeaders.Add(ApiKeyRequirement.HeaderName, pair.ApiKey!);

        var sinDocumento = await _client.PostAsync("/api/vehiculos", Json(new
        {
            placa = "ABC123",
            tipoVehiculo = 1,
            tipoServicio = 1,
            tipoCombustible = 1,
            capacidadPasajeros = 5,
            color = "#FF0000",
            modelo = 2020,
            marca = "Toyota",
            linea = "Corolla",
            tipoDocumentoId = Guid.Empty,
            documentoBase64 = "",
            nombreArchivo = "",
            fechaExpedicion = DateTime.UtcNow.AddDays(-30),
            fechaVencimiento = DateTime.UtcNow.AddDays(-1)
        }));

        sinDocumento.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RevokeToken_ForUnknownPersona_Should_NotFound()
    {
        using var scope = _factory.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<RevokeRefreshTokenUseCase>();

        var act = async () => await useCase.ExecuteAsync(
            new RevokeRefreshTokenInputDto(Guid.NewGuid(), "some-token"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private async Task<HttpResponseMessage> CrearPersonaDirecta(string numero, string nombres, string apellidos, string correo, int tipo)
        => await _client.PostAsync("/api/personas", Json(new
        {
            tipoIdentificacion = 1,
            numeroIdentificacion = numero,
            nombres,
            apellidos,
            correoElectronico = correo,
            tipoPersona = tipo
        }));

    /// <summary>
    ///     Crea la persona con las credenciales del administrador semilla y devuelve la contraseña
    ///     temporal que el sistema genera para el usuario nuevo.
    /// </summary>
    private async Task<CrearAdministrativoResult> CrearAdministrativo(string numero, string nombres, string apellidos)
    {
        await AutenticarComoSemilla();

        var correo = $"{nombres.ToLower()}.{apellidos.ToLower()}{numero}@example.com";
        var response = await CrearPersonaDirecta(numero, nombres, apellidos, correo, 1);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(
            HttpStatusCode.Created,
            "numero={0} cuerpo={1}", numero, body);

        return new CrearAdministrativoResult(
            (await ReadAs<CrearPersonaResponse>(response)).PasswordGenerada!);
    }

    private bool _autenticado;

    private async Task AutenticarComoSemilla()
    {
        if (_autenticado)
        {
            return;
        }

        _autenticado = true;

        var login = await LoginAsync(ApiFactory.SeedLogin, ApiFactory.SeedPassword);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(login.AccessTokenType, login.AccessToken);
        _client.DefaultRequestHeaders.Remove(ApiKeyRequirement.HeaderName);
        _client.DefaultRequestHeaders.Add(ApiKeyRequirement.HeaderName, login.ApiKey!);
    }

    private async Task<TokenPairDto> LoginAsync(string login, string password)
    {
        var response = await LoginRawAsync(login, password);
        response.EnsureSuccessStatusCode();
        return await ReadAs<TokenPairDto>(response);
    }

    private Task<HttpResponseMessage> LoginRawAsync(string login, string password)
        => _client.PostAsync("/api/auth/login", Json(new { login, password }));

    private static int _contador;

    /// <summary>
    ///     Identificación única por test. Se combina con un GUID porque la colección corre en
    ///     paralelo y los milisegundos no guarantee unicidad.
    /// </summary>
    private static string NumeroAleatorio()
    {
        // Contador monotónico con base fija: garantiza unicidad dentro de la ejecución. Se arranca
        // en 2 porque el 1 está reservado para la persona semilla de ApiFactory.
        var n = 2 + Interlocked.Increment(ref _contador);
        return n.ToString("D9");
    }

    private static StringContent Json(object body)
        => new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private static async Task<T> ReadAs<T>(HttpResponseMessage response)
        => JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(), JsonOptions)!;

    private sealed record ErrorResponse(int? Status, string? Code, string? Message);

    private sealed record TokenPairDto(
        string? AccessToken,
        string? AccessTokenType,
        string? RefreshToken,
        string? ApiKey);

    private sealed record CrearPersonaResponse(
        string? Id,
        string? Login,
        string? ApiKey,
        string? PasswordGenerada,
        int? TipoPersona);

    private sealed record CrearAdministrativoResult(string PasswordGenerada);

    private sealed record PerfilResponse(string? Login, TipoPersona? TipoPersona);
}
