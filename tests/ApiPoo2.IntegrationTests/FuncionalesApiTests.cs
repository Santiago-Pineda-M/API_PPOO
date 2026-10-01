using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ApiPoo2.Domain.Documentos;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Vehiculos;
using ApiPoo2.WebApi.Auth;
using FluentAssertions;

namespace ApiPoo2.IntegrationTests;

[Collection("API")]
public sealed class FuncionalesApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _client;

    public FuncionalesApiTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Persona_GetYUpdate()
    {
        await AutenticarComoSemillaAsync();

        var numero = NumeroUnico();
        var creado = await CrearPersonaAsync(numero, "Rosa", "Mora", $"rosa.mora{numero}@example.com", 1);
        creado.Login.Should().StartWith("RM");

        var detalle = await _client.GetAsync($"/api/personas/{creado.Id}");
        detalle.StatusCode.Should().Be(HttpStatusCode.OK);
        var persona = await ReadAs<PersonaDto>(detalle);
        persona.CorreoElectronico.Should().Contain(numero);

        var actualizado = await _client.PutAsync(
            $"/api/personas/{creado.Id}",
            Json(new
            {
                nombres = "Rosa María",
                apellidos = "Mora López",
                correoElectronico = $"rosa.mora.actualizada{numero}@example.com",
                tipoPersona = 1
            }));
        actualizado.StatusCode.Should().Be(HttpStatusCode.OK);

        var verificado = await ReadAs<PersonaDto>(await _client.GetAsync($"/api/personas/{creado.Id}"));
        verificado.Nombres.Should().Be("Rosa María");
        verificado.CorreoElectronico.Should().Contain("actualizada");
    }

    [Fact]
    public async Task Persona_UpdateRechazaCorreoDuplicado()
    {
        await AutenticarComoSemillaAsync();

        var primerNumero = NumeroUnico();
        var primerCorreo = $"ana.ruiz{primerNumero}@example.com";
        var primero = await CrearPersonaAsync(primerNumero, "Ana", "Ruiz", primerCorreo, 1);
        var segundoNumero = NumeroUnico();
        var segundo = await CrearPersonaAsync(segundoNumero, "Luis", "Paz", $"luis.paz{segundoNumero}@example.com", 1);

        var duplicado = await _client.PutAsync(
            $"/api/personas/{segundo.Id}",
            Json(new
            {
                nombres = "Luis",
                apellidos = "Paz",
            correoElectronico = primerCorreo,
            tipoPersona = 1
        }));

        duplicado.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DocumentoParametrico_Crud()
    {
        await AutenticarComoSemillaAsync();
        var codigo = $"DOC{NumeroCorto()}";

        var creado = await _client.PostAsync("/api/documentos", Json(new
        {
            codigo,
            nombre = "Documento funcional",
            tiposVehiculoAplicables = "AM",
            codigoObligatoriedad = "RR",
            descripcion = "Catálogo para pruebas funcionales"
        }));
        creado.StatusCode.Should().Be(HttpStatusCode.Created);
        var documento = await ReadAs<DocumentoDto>(creado);

        var listado = await _client.GetAsync("/api/documentos");
        listado.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAs<List<DocumentoDto>>(listado)).Should().Contain(d => d.Codigo == codigo);

        var detalle = await _client.GetAsync($"/api/documentos/{documento.Id}");
        detalle.StatusCode.Should().Be(HttpStatusCode.OK);

        var actualizado = await _client.PutAsync($"/api/documentos/{documento.Id}", Json(new
        {
            nombre = "Documento funcional actualizado",
            tiposVehiculoAplicables = "A",
            codigoObligatoriedad = "RA",
            descripcion = "Solo automóviles"
        }));
        actualizado.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAs<DocumentoDto>(actualizado)).Nombre.Should().Contain("actualizado");

        var eliminado = await _client.DeleteAsync($"/api/documentos/{documento.Id}");
        eliminado.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetAsync($"/api/documentos/{documento.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Vehiculo_CrudBusquedasYDocumentos()
    {
        await AutenticarComoSemillaAsync();
        var codigo = $"SOAT{NumeroCorto()}";
        var documentoId = await RegistrarDocumentoAsync(codigo, "A", "RA");
        var placa = PlacaUnica();

        var vehiculo = await CrearVehiculoAsync(placa, documentoId);
        var detalle = await _client.GetAsync($"/api/vehiculos/{vehiculo.Id}");
        detalle.StatusCode.Should().Be(HttpStatusCode.OK);

        var actualizado = await _client.PutAsync($"/api/vehiculos/{vehiculo.Id}", Json(new
        {
            placa,
            tipoVehiculo = 1,
            tipoServicio = 2,
            tipoCombustible = 1,
            capacidadPasajeros = 5,
            color = "#112233",
            modelo = 2021,
            marca = "Toyota",
            linea = "Corolla"
        }));
        actualizado.StatusCode.Should().Be(HttpStatusCode.OK);

        var porTipo = await _client.GetAsync("/api/vehiculos/tipo/1");
        porTipo.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAs<List<VehiculoResumenDto>>(porTipo)).Should().Contain(v => v.Id == vehiculo.Id);

        var porDocumento = await _client.GetAsync($"/api/vehiculos/documento/{codigo}");
        porDocumento.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAs<List<VehiculoResumenDto>>(porDocumento)).Should().Contain(v => v.Id == vehiculo.Id);

        var enVerificacion = await _client.GetAsync("/api/vehiculos/estado-documento/3");
        enVerificacion.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAs<List<VehiculoResumenDto>>(enVerificacion)).Should().Contain(v => v.Id == vehiculo.Id);

        var habilitar = await _client.PutAsync(
            $"/api/vehiculos/{vehiculo.Id}/documentos/{documentoId}/estado",
            Json(new { estado = 1 }));
        habilitar.StatusCode.Should().Be(HttpStatusCode.OK);

        var habilitados = await _client.GetAsync("/api/vehiculos/estado-documento/1");
        habilitados.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAs<List<VehiculoResumenDto>>(habilitados)).Should().Contain(v => v.Id == vehiculo.Id);

        var usado = await _client.DeleteAsync($"/api/documentos/{documentoId}");
        usado.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var eliminado = await _client.DeleteAsync($"/api/vehiculos/{vehiculo.Id}");
        eliminado.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetAsync($"/api/vehiculos/{vehiculo.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var liberarCatalogo = await _client.DeleteAsync($"/api/documentos/{documentoId}");
        liberarCatalogo.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task AutenticarComoSemillaAsync()
    {
        var login = await LoginAsync(ApiFactory.SeedLogin, ApiFactory.SeedPassword);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(login.AccessTokenType, login.AccessToken);
        _client.DefaultRequestHeaders.Remove(ApiKeyRequirement.HeaderName);
        _client.DefaultRequestHeaders.Add(ApiKeyRequirement.HeaderName, login.ApiKey!);
    }

    private async Task<TokenDto> LoginAsync(string login, string password)
    {
        var response = await _client.PostAsync("/api/auth/login", Json(new { login, password }));
        response.EnsureSuccessStatusCode();
        return await ReadAs<TokenDto>(response);
    }

    private async Task<PersonaCreadaDto> CrearPersonaAsync(string numero, string nombres, string apellidos, string correo, int tipo)
    {
        var response = await _client.PostAsync("/api/personas", Json(new
        {
            tipoIdentificacion = 1,
            numeroIdentificacion = numero,
            nombres,
            apellidos,
            correoElectronico = correo,
            tipoPersona = tipo
        }));
        response.EnsureSuccessStatusCode();
        return await ReadAs<PersonaCreadaDto>(response);
    }

    private async Task<Guid> RegistrarDocumentoAsync(string codigo, string tipos, string obligatoriedad)
    {
        var response = await _client.PostAsync("/api/documentos", Json(new
        {
            codigo,
            nombre = $"Documento {codigo}",
            tiposVehiculoAplicables = tipos,
            codigoObligatoriedad = obligatoriedad,
            descripcion = "Documento funcional"
        }));
        response.EnsureSuccessStatusCode();
        return (await ReadAs<DocumentoDto>(response)).Id;
    }

    private async Task<VehiculoCreadoDto> CrearVehiculoAsync(string placa, Guid documentoId)
    {
        var response = await _client.PostAsync("/api/vehiculos", Json(new
        {
            placa,
            tipoVehiculo = 1,
            tipoServicio = 2,
            tipoCombustible = 1,
            capacidadPasajeros = 5,
            color = "#FF0000",
            modelo = 2020,
            marca = "Toyota",
            linea = "Corolla",
            documentoId,
            documentoBase64 = Convert.ToBase64String("%PDF-1.4 funcional"u8.ToArray()),
            nombreArchivo = "soat.pdf",
            fechaExpedicion = DateTime.UtcNow.AddDays(-10),
            fechaVencimiento = DateTime.UtcNow.AddDays(30)
        }));
        response.EnsureSuccessStatusCode();
        return await ReadAs<VehiculoCreadoDto>(response);
    }

    private static int _contador = 900_000_000;

    private static string NumeroUnico() => Interlocked.Increment(ref _contador).ToString("D9");

    private static string NumeroCorto() => Interlocked.Increment(ref _contador).ToString()[^6..];

    private static string PlacaUnica() => "QZX" + Interlocked.Increment(ref _contador).ToString()[^3..];

    private static StringContent Json(object body)
        => new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private static async Task<T> ReadAs<T>(HttpResponseMessage response)
        => JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(), JsonOptions)!;

    private sealed record TokenDto(string? AccessToken, string? AccessTokenType, string? ApiKey);

    private sealed record PersonaCreadaDto(Guid Id, string? Login, string? PasswordGenerada, string? NumeroIdentificacion);

    private sealed record PersonaDto(Guid Id, string? Nombres, string? CorreoElectronico, TipoPersona? TipoPersona);

    private sealed record DocumentoDto(Guid Id, string? Codigo, string? Nombre);

    private sealed record VehiculoCreadoDto(Guid Id);

    private sealed record VehiculoResumenDto(Guid Id, string? Placa, TipoVehiculo? TipoVehiculo, int? TipoServicio);
}
