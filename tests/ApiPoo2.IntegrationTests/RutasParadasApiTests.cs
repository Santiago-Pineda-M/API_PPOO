using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ApiPoo2.WebApi.Auth;
using FluentAssertions;

namespace ApiPoo2.IntegrationTests;

[Collection("API")]
public sealed class RutasParadasApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static int _contador = 700_000_000;

    private readonly HttpClient _client;

    public RutasParadasApiTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Ruta_CreaConParadasInicialYFinalYPermiteCrudCompleto()
    {
        await AutenticarComoSemillaAsync();
        var contexto = await PrepararConductorAptoAsync();
        var codigo = CodigoUnico();

        var creacion = await _client.PostAsync("/api/rutas", Json(new
        {
            codigoRuta = codigo,
            personaId = contexto.PersonaId,
            vehiculoId = contexto.VehiculoId,
            ubicacionInicial = "Terminal Norte",
            ubicacionFinal = "Destino",
            latitud = 6.2442,
            longitud = -75.5812
        }));

        creacion.StatusCode.Should().Be(
            HttpStatusCode.Created, await creacion.Content.ReadAsStringAsync());
        creacion.Headers.Location!.ToString().Should().Contain($"/api/rutas/{codigo}");

        var creada = await ReadAs<RutaDto>(creacion);
        creada.CodigoRuta.Should().Be(codigo);
        creada.Operable.Should().BeTrue();
        creada.Paradas.Select(p => p.OrdenParada).Should().Equal(0, 1);
        creada.Paradas.Single(p => p.OrdenParada == 0).Ubicacion.Should().Be("Terminal Norte");
        creada.Paradas.Single(p => p.OrdenParada == 1).Ubicacion.Should().Be("Destino");

        var detalle = await _client.GetAsync($"/api/rutas/{codigo}");
        detalle.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAs<RutaDto>(detalle)).Paradas.Should().HaveCount(2);

        var agregada = await _client.PostAsync($"/api/rutas/{codigo}/paradas", Json(new
        {
            ubicacion = "Parque Principal",
            ordenParada = 2,
            latitud = 6.2518,
            longitud = -75.5636
        }));

        agregada.StatusCode.Should().Be(HttpStatusCode.Created, await agregada.Content.ReadAsStringAsync());
        agregada.Headers.Location!.ToString().Should().Contain("/api/paradas/");

        var parada = await ReadAs<ParadaDto>(agregada);
        parada.OrdenParada.Should().Be(2);

        var listadoParadas = await ReadAs<List<ParadaDto>>(
            await _client.GetAsync($"/api/rutas/{codigo}/paradas"));

        listadoParadas.Should().HaveCount(3);
        listadoParadas.Select(p => p.OrdenParada).Should().BeInAscendingOrder();

        (await ReadAs<List<RutaResumenDto>>(await _client.GetAsync($"/api/rutas?codigo={codigo}")))
            .Should().Contain(r => r.CodigoRuta == codigo);
        (await ReadAs<List<RutaResumenDto>>(await _client.GetAsync($"/api/rutas?placa={contexto.Placa}")))
            .Should().Contain(r => r.CodigoRuta == codigo);
        (await ReadAs<List<RutaResumenDto>>(
                await _client.GetAsync($"/api/rutas?numeroIdentificacion={contexto.NumeroIdentificacion}")))
            .Should().Contain(r => r.CodigoRuta == codigo);
        (await ReadAs<List<RutaResumenDto>>(await _client.GetAsync("/api/rutas?operable=true")))
            .Should().Contain(r => r.CodigoRuta == codigo);

        var consultaParada = await _client.GetAsync($"/api/paradas/{parada.Id}");
        consultaParada.StatusCode.Should().Be(HttpStatusCode.OK);

        var actualizada = await _client.PutAsync($"/api/paradas/{parada.Id}", Json(new
        {
            ubicacion = "Parque de los Deseos",
            ordenParada = 2,
            latitud = 6.2697,
            longitud = -75.5658
        }));

        actualizada.StatusCode.Should().Be(HttpStatusCode.OK);
        var paradaActualizada = await ReadAs<ParadaDto>(actualizada);
        paradaActualizada.OrdenParada.Should().Be(2);
        paradaActualizada.Ubicacion.Should().Be("Parque de los Deseos");

        (await _client.DeleteAsync($"/api/paradas/{parada.Id}")).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        (await _client.GetAsync($"/api/paradas/{parada.Id}")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);

        (await _client.DeleteAsync($"/api/rutas/{codigo}")).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        var eliminada = await _client.GetAsync($"/api/rutas/{codigo}");
        eliminada.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await eliminada.Content.ReadAsStringAsync()).Should().Contain("ruta.not_found");
    }

    [Fact]
    public async Task Ruta_RechazaCodigoDuplicado()
    {
        await AutenticarComoSemillaAsync();
        var contexto = await PrepararConductorAptoAsync();
        var codigo = CodigoUnico();

        var primera = await _client.PostAsync("/api/rutas", Json(new
        {
            codigoRuta = codigo,
            personaId = contexto.PersonaId,
            vehiculoId = contexto.VehiculoId,
            ubicacionInicial = "Origen",
            ubicacionFinal = "Destino",
        }));
        primera.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicada = await _client.PostAsync("/api/rutas", Json(new
        {
            codigoRuta = codigo,
            personaId = contexto.PersonaId,
            vehiculoId = contexto.VehiculoId,
            ubicacionInicial = "Origen duplicado",
            ubicacionFinal = "Destino",
        }));

        duplicada.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await duplicada.Content.ReadAsStringAsync()).Should().Contain("ruta.codigo.conflict");
    }

    [Fact]
    public async Task Ruta_RechazaConductorSinLicenciaVigente()
    {
        await AutenticarComoSemillaAsync();
        var contexto = await PrepararConductorAptoAsync(registrarLicencia: false);

        var respuesta = await _client.PostAsync("/api/rutas", Json(new
        {
            codigoRuta = CodigoUnico(),
            personaId = contexto.PersonaId,
            vehiculoId = contexto.VehiculoId,
            ubicacionInicial = "Origen",
            ubicacionFinal = "Destino",
        }));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await respuesta.Content.ReadAsStringAsync()).Should().Contain("ruta.licencia.vencida");
    }

    [Fact]
    public async Task Ruta_RechazaPersonaQueNoEsConductora()
    {
        await AutenticarComoSemillaAsync();
        var tipoDocumentoId = await RegistrarTipoDocumentoAsync();
        var placa = PlacaUnica();
        var vehiculo = await CrearVehiculoAsync(placa, tipoDocumentoId);
        await HabilitarDocumentoAsync(vehiculo.Id, tipoDocumentoId);

        var numero = NumeroUnico();
        var administrativo = await CrearPersonaAsync(numero, "Admin", "Rutas", 1);

        var respuesta = await _client.PostAsync("/api/rutas", Json(new
        {
            codigoRuta = CodigoUnico(),
            personaId = administrativo.Id,
            vehiculoId = vehiculo.Id,
            ubicacionInicial = "Origen",
            ubicacionFinal = "Destino",
        }));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await respuesta.Content.ReadAsStringAsync()).Should().Contain("ruta.conductor.invalido");
    }

    [Fact]
    public async Task Ruta_ReasignaConductorYVehiculo()
    {
        await AutenticarComoSemillaAsync();
        var original = await PrepararConductorAptoAsync();
        var reemplazo = await PrepararConductorAptoAsync();
        var codigo = CodigoUnico();

        var creacion = await _client.PostAsync("/api/rutas", Json(new
        {
            codigoRuta = codigo,
            personaId = original.PersonaId,
            vehiculoId = original.VehiculoId,
            ubicacionInicial = "Terminal Norte",
            ubicacionFinal = "Destino",
        }));
        creacion.StatusCode.Should().Be(HttpStatusCode.Created);

        var reasignada = await _client.PutAsync($"/api/rutas/{codigo}", Json(new
        {
            personaId = reemplazo.PersonaId,
            vehiculoId = reemplazo.VehiculoId
        }));

        reasignada.StatusCode.Should().Be(HttpStatusCode.OK, await reasignada.Content.ReadAsStringAsync());

        var cuerpo = await ReadAs<RutaDto>(reasignada);
        cuerpo.CodigoRuta.Should().Be(codigo);
        cuerpo.PersonaId.Should().Be(reemplazo.PersonaId);
        cuerpo.VehiculoId.Should().Be(reemplazo.VehiculoId);
        cuerpo.Placa.Should().Be(reemplazo.Placa);
        cuerpo.Operable.Should().BeTrue();
        cuerpo.Paradas.Should().ContainSingle(p => p.OrdenParada == 0);

        var persistida = await ReadAs<RutaDto>(await _client.GetAsync($"/api/rutas/{codigo}"));
        persistida.PersonaId.Should().Be(reemplazo.PersonaId);
        persistida.VehiculoId.Should().Be(reemplazo.VehiculoId);
        persistida.NumeroIdentificacionConductor.Should().Be(reemplazo.NumeroIdentificacion);
    }

    [Fact]
    public async Task Ruta_Reasigna_RechazaParejaNoOperable()
    {
        await AutenticarComoSemillaAsync();
        var original = await PrepararConductorAptoAsync();
        var otroVehiculo = await PrepararConductorAptoAsync();
        var codigo = CodigoUnico();

        var creacion = await _client.PostAsync("/api/rutas", Json(new
        {
            codigoRuta = codigo,
            personaId = original.PersonaId,
            vehiculoId = original.VehiculoId,
            ubicacionInicial = "Origen",
            ubicacionFinal = "Destino",
        }));
        creacion.StatusCode.Should().Be(HttpStatusCode.Created);

        // El conductor existe con licencia vigente, pero no está asociado a este vehículo.
        var respuesta = await _client.PutAsync($"/api/rutas/{codigo}", Json(new
        {
            personaId = original.PersonaId,
            vehiculoId = otroVehiculo.VehiculoId
        }));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await respuesta.Content.ReadAsStringAsync()).Should().Contain("ruta.conductor.no_operable");
    }

    [Fact]
    public async Task Ruta_Reasigna_RechazaPersonaQueNoEsConductora()
    {
        await AutenticarComoSemillaAsync();
        var original = await PrepararConductorAptoAsync();
        var codigo = CodigoUnico();

        var creacion = await _client.PostAsync("/api/rutas", Json(new
        {
            codigoRuta = codigo,
            personaId = original.PersonaId,
            vehiculoId = original.VehiculoId,
            ubicacionInicial = "Origen",
            ubicacionFinal = "Destino",
        }));
        creacion.StatusCode.Should().Be(HttpStatusCode.Created);

        var administrativo = await CrearPersonaAsync(NumeroUnico(), "Admin", "Reasigna", 1);

        var respuesta = await _client.PutAsync($"/api/rutas/{codigo}", Json(new
        {
            personaId = administrativo.Id,
            vehiculoId = original.VehiculoId
        }));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await respuesta.Content.ReadAsStringAsync()).Should().Contain("ruta.conductor.invalido");

        var sinCambios = await ReadAs<RutaDto>(await _client.GetAsync($"/api/rutas/{codigo}"));
        sinCambios.PersonaId.Should().Be(original.PersonaId);
    }

    [Fact]
    public async Task Ruta_Reasigna_RechazaConductorSinLicenciaVigente()
    {
        await AutenticarComoSemillaAsync();
        var original = await PrepararConductorAptoAsync();
        var sinLicencia = await PrepararConductorAptoAsync(registrarLicencia: false);
        var codigo = CodigoUnico();

        var creacion = await _client.PostAsync("/api/rutas", Json(new
        {
            codigoRuta = codigo,
            personaId = original.PersonaId,
            vehiculoId = original.VehiculoId,
            ubicacionInicial = "Origen",
            ubicacionFinal = "Destino",
        }));
        creacion.StatusCode.Should().Be(HttpStatusCode.Created);

        // Es CONDUCTOR, pero no tiene licencia registrada.
        var respuesta = await _client.PutAsync($"/api/rutas/{codigo}", Json(new
        {
            personaId = sinLicencia.PersonaId,
            vehiculoId = sinLicencia.VehiculoId
        }));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await respuesta.Content.ReadAsStringAsync()).Should().Contain("ruta.licencia.vencida");

        var sinCambios = await ReadAs<RutaDto>(await _client.GetAsync($"/api/rutas/{codigo}"));
        sinCambios.PersonaId.Should().Be(original.PersonaId);
    }

    [Fact]
    public async Task Parada_RechazaOrdenDuplicado()
    {
        await AutenticarComoSemillaAsync();
        var contexto = await PrepararConductorAptoAsync();
        var codigo = CodigoUnico();

        var creacion = await _client.PostAsync("/api/rutas", Json(new
        {
            codigoRuta = codigo,
            personaId = contexto.PersonaId,
            vehiculoId = contexto.VehiculoId,
            ubicacionInicial = "Origen",
            ubicacionFinal = "Destino",
        }));
        creacion.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicada = await _client.PostAsync($"/api/rutas/{codigo}/paradas", Json(new
        {
            ubicacion = "Otra parada inicial",
            ordenParada = 0,
        }));

        duplicada.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await duplicada.Content.ReadAsStringAsync()).Should().Contain("parada.orden.duplicado");
    }

    [Fact]
    public async Task Parada_RechazaEliminarLaInicial()
    {
        await AutenticarComoSemillaAsync();
        var contexto = await PrepararConductorAptoAsync();
        var codigo = CodigoUnico();

        var creacion = await _client.PostAsync("/api/rutas", Json(new
        {
            codigoRuta = codigo,
            personaId = contexto.PersonaId,
            vehiculoId = contexto.VehiculoId,
            ubicacionInicial = "Origen",
            ubicacionFinal = "Destino",
        }));
        creacion.StatusCode.Should().Be(HttpStatusCode.Created);

        var creada = await ReadAs<RutaDto>(creacion);
        var inicial = creada.Paradas.Single(p => p.OrdenParada == 0);

        var eliminacion = await _client.DeleteAsync($"/api/paradas/{inicial.Id}");

        eliminacion.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await eliminacion.Content.ReadAsStringAsync()).Should().Contain("parada.inicial.requerida");
    }

    [Fact]
    public async Task Parada_RechazaCoordenadasIncompletas()
    {
        await AutenticarComoSemillaAsync();
        var contexto = await PrepararConductorAptoAsync();
        var codigo = CodigoUnico();

        var creacion = await _client.PostAsync("/api/rutas", Json(new
        {
            codigoRuta = codigo,
            personaId = contexto.PersonaId,
            vehiculoId = contexto.VehiculoId,
            ubicacionInicial = "Origen",
            ubicacionFinal = "Destino",
        }));
        creacion.StatusCode.Should().Be(HttpStatusCode.Created);

        var respuesta = await _client.PostAsync($"/api/rutas/{codigo}/paradas", Json(new
        {
            ubicacion = "Sin longitud",
            ordenParada = 2,
            latitud = 6.24,
        }));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ruta_ConsultasPorConductorYPlacaDevuelvenSoloLoPedido()
    {
        await AutenticarComoSemillaAsync();
        var contexto = await PrepararConductorAptoAsync();
        var codigo = CodigoUnico();

        var creacion = await _client.PostAsync("/api/rutas", Json(new
        {
            codigoRuta = codigo,
            personaId = contexto.PersonaId,
            vehiculoId = contexto.VehiculoId,
            ubicacionInicial = "Origen",
            ubicacionFinal = "Destino",
        }));
        creacion.StatusCode.Should().Be(HttpStatusCode.Created);

        // Las consultas de rutas exigen token, no APIKey: se retira el encabezado a propósito.
        _client.DefaultRequestHeaders.Remove(ApiKeyRequirement.HeaderName);

        var porConductor = await _client.GetAsync(
            $"/api/rutas/conductor/{contexto.NumeroIdentificacion}");

        porConductor.StatusCode.Should().Be(
            HttpStatusCode.OK, await porConductor.Content.ReadAsStringAsync());
        (await ReadAs<List<string>>(porConductor)).Should().Equal(codigo);

        var porPlaca = await _client.GetAsync($"/api/rutas/placa/{contexto.Placa}");

        porPlaca.StatusCode.Should().Be(HttpStatusCode.OK, await porPlaca.Content.ReadAsStringAsync());
        var parejas = await ReadAs<List<RutaConductorDto>>(porPlaca);
        parejas.Should().ContainSingle().Which.CodigoRuta.Should().Be(codigo);
        parejas.Single().Conductor.Should().NotBeNullOrWhiteSpace();
    }

    private sealed record ContextoRuta(Guid PersonaId, Guid VehiculoId, string Placa, string NumeroIdentificacion);

    private sealed record RutaConductorDto(string CodigoRuta, string Conductor);

    private async Task<ContextoRuta> PrepararConductorAptoAsync(bool registrarLicencia = true)
    {
        var tipoDocumentoId = await RegistrarTipoDocumentoAsync();
        var placa = PlacaUnica();
        var vehiculo = await CrearVehiculoAsync(placa, tipoDocumentoId);
        await HabilitarDocumentoAsync(vehiculo.Id, tipoDocumentoId);

        var numero = NumeroUnico();
        var conductor = await CrearPersonaAsync(numero, "Conductor", "Apto", 2);

        if (registrarLicencia)
        {
            await RegistrarLicenciaAsync(conductor.Id);

            var asociacion = await _client.PostAsync("/api/conductores/vehiculos", Json(new
            {
                personaId = conductor.Id,
                vehiculoIds = new[] { vehiculo.Id }
            }));
            asociacion.EnsureSuccessStatusCode();

            // Toda asociación nace en EA (espera de aprobación); para operar debe quedar en PO.
            var habilitacion = await _client.PutAsync("/api/conductores/estado", Json(new
            {
                personaId = conductor.Id,
                vehiculoId = vehiculo.Id,
                estado = 1
            }));
            habilitacion.EnsureSuccessStatusCode();
        }

        return new ContextoRuta(conductor.Id, vehiculo.Id, placa, numero);
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

    private async Task<PersonaCreadaDto> CrearPersonaAsync(
        string numero,
        string nombres,
        string apellidos,
        int tipoPersona)
    {
        var response = await _client.PostAsync("/api/personas", Json(new
        {
            tipoIdentificacion = 1,
            numeroIdentificacion = numero,
            nombres,
            apellidos,
            correoElectronico = $"{nombres.ToLowerInvariant()}.{numero}@example.com",
            tipoPersona
        }));

        response.EnsureSuccessStatusCode();
        return await ReadAs<PersonaCreadaDto>(response);
    }

    private async Task RegistrarLicenciaAsync(Guid personaId)
    {
        var response = await _client.PutAsync($"/api/personas/{personaId}/licencia", Json(new
        {
            contenidoBase64 = Convert.ToBase64String("%PDF-1.4 licencia"u8.ToArray()),
            fechaVigencia = DateTime.UtcNow.AddYears(1)
        }));

        response.EnsureSuccessStatusCode();
    }

    private async Task<Guid> RegistrarTipoDocumentoAsync()
    {
        var codigo = $"RT{NumeroCorto()}";

        var response = await _client.PostAsync("/api/tipos-documento", Json(new
        {
            codigo,
            nombre = $"TipoDocumento {codigo}",
            tiposVehiculoAplicables = "A",
            codigoObligatoriedad = "RA",
            descripcion = "TipoDocumento para pruebas de rutas"
        }));

        response.EnsureSuccessStatusCode();
        return (await ReadAs<TipoDocumentoDto>(response)).Id;
    }

    private async Task<VehiculoCreadoDto> CrearVehiculoAsync(string placa, Guid tipoDocumentoId)
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
            tipoDocumentoId,
            documentoBase64 = Convert.ToBase64String("%PDF-1.4 rutas"u8.ToArray()),
            nombreArchivo = "soat.pdf",
            fechaExpedicion = DateTime.UtcNow.AddDays(-10),
            fechaVencimiento = DateTime.UtcNow.AddDays(30)
        }));

        response.EnsureSuccessStatusCode();
        return await ReadAs<VehiculoCreadoDto>(response);
    }

    private async Task HabilitarDocumentoAsync(Guid vehiculoId, Guid tipoDocumentoId)
    {
        var response = await _client.PutAsync(
            $"/api/vehiculos/{vehiculoId}/documentos/{tipoDocumentoId}/estado",
            Json(new { estado = 1 }));

        response.EnsureSuccessStatusCode();
    }

    private static string NumeroUnico() => Interlocked.Increment(ref _contador).ToString("D9");

    private static string NumeroCorto() => Interlocked.Increment(ref _contador).ToString()[^6..];

    private static string PlacaUnica() => "RTZ" + Interlocked.Increment(ref _contador).ToString()[^3..];

    private static string CodigoUnico() => $"RUTA{Interlocked.Increment(ref _contador)}";

    private static StringContent Json(object body)
        => new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private static async Task<T> ReadAs<T>(HttpResponseMessage response)
        => JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(), JsonOptions)!;

    private sealed record TokenDto(string? AccessToken, string? AccessTokenType, string? ApiKey);

    private sealed record PersonaCreadaDto(Guid Id);

    private sealed record TipoDocumentoDto(Guid Id);

    private sealed record VehiculoCreadoDto(Guid Id);

    private sealed record RutaDto(
        Guid Id,
        string CodigoRuta,
        Guid PersonaId,
        string NumeroIdentificacionConductor,
        Guid VehiculoId,
        string Placa,
        bool Operable,
        List<ParadaDto> Paradas);

    private sealed record RutaResumenDto(Guid Id, string CodigoRuta, string Placa, bool Operable);

    private sealed record ParadaDto(
        Guid Id,
        Guid IdRuta,
        string Ubicacion,
        int OrdenParada,
        double? Latitud,
        double? Longitud);
}
