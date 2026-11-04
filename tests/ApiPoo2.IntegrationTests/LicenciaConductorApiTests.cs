using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ApiPoo2.WebApi.Auth;
using FluentAssertions;

namespace ApiPoo2.IntegrationTests;

[Collection("API")]
public sealed class LicenciaConductorApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static int _contador = 600_000_000;

    private readonly HttpClient _client;

    public LicenciaConductorApiTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Licencia_RegistrarConsultarYEliminar()
    {
        await AutenticarComoSemillaAsync();
        var conductor = await CrearPersonaAsync(2);
        var vigencia = DateTime.UtcNow.AddYears(1).Date;

        var registro = await _client.PutAsync($"/api/personas/{conductor.Id}/licencia", Json(new
        {
            contenidoBase64 = Convert.ToBase64String("%PDF-1.4 licencia conductor"u8.ToArray()),
            fechaVigencia = vigencia
        }));

        registro.StatusCode.Should().Be(HttpStatusCode.OK);
        var registrada = await ReadAs<LicenciaDto>(registro);
        registrada.PersonaId.Should().Be(conductor.Id);
        registrada.Vigente.Should().BeTrue();
        registrada.LongitudBytes.Should().BeGreaterThan(0);
        registrada.FechaVigencia.Date.Should().Be(vigencia);

        var consulta = await _client.GetAsync($"/api/personas/{conductor.Id}/licencia");
        consulta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAs<LicenciaDto>(consulta)).PersonaId.Should().Be(conductor.Id);

        (await _client.DeleteAsync($"/api/personas/{conductor.Id}/licencia")).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        var despues = await _client.GetAsync($"/api/personas/{conductor.Id}/licencia");
        despues.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await despues.Content.ReadAsStringAsync()).Should().Contain("persona.licencia.not_found");
    }

    [Fact]
    public async Task Licencia_RechazaPersonaAdministrativa()
    {
        await AutenticarComoSemillaAsync();
        var administrativo = await CrearPersonaAsync(1);

        var respuesta = await _client.PutAsync($"/api/personas/{administrativo.Id}/licencia", Json(new
        {
            contenidoBase64 = Convert.ToBase64String("%PDF-1.4 licencia"u8.ToArray()),
            fechaVigencia = DateTime.UtcNow.AddYears(1)
        }));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await respuesta.Content.ReadAsStringAsync()).Should().Contain("persona.licencia.tipo_persona");
    }

    private async Task AutenticarComoSemillaAsync()
    {
        var response = await _client.PostAsync(
            "/api/auth/login",
            Json(new { login = ApiFactory.SeedLogin, password = ApiFactory.SeedPassword }));

        response.EnsureSuccessStatusCode();
        var login = await ReadAs<TokenDto>(response);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(login.AccessTokenType, login.AccessToken);
        _client.DefaultRequestHeaders.Remove(ApiKeyRequirement.HeaderName);
        _client.DefaultRequestHeaders.Add(ApiKeyRequirement.HeaderName, login.ApiKey!);
    }

    private async Task<PersonaCreadaDto> CrearPersonaAsync(int tipoPersona)
    {
        var numero = Interlocked.Increment(ref _contador).ToString("D9");

        var response = await _client.PostAsync("/api/personas", Json(new
        {
            tipoIdentificacion = 1,
            numeroIdentificacion = numero,
            nombres = "Licencia",
            apellidos = "Prueba",
            correoElectronico = $"licencia.{numero}@example.com",
            tipoPersona
        }));

        response.EnsureSuccessStatusCode();
        return await ReadAs<PersonaCreadaDto>(response);
    }

    private static StringContent Json(object body)
        => new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private static async Task<T> ReadAs<T>(HttpResponseMessage response)
        => JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(), JsonOptions)!;

    private sealed record TokenDto(string? AccessToken, string? AccessTokenType, string? ApiKey);

    private sealed record PersonaCreadaDto(Guid Id);

    private sealed record LicenciaDto(
        Guid PersonaId,
        DateTime FechaVigencia,
        bool Vigente,
        int LongitudBytes);
}
