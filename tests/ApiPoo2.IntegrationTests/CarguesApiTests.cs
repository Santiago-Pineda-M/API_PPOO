using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ApiPoo2.Domain.Trayectos;
using ApiPoo2.WebApi.Auth;
using ClosedXML.Excel;
using FluentAssertions;

namespace ApiPoo2.IntegrationTests;

[Collection("API")]
public sealed class CarguesApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly string[] Encabezados =
    [
        "codigo_ruta",
        "identificacion_conductor",
        "placa_vehiculo",
        "ubicacion",
        "orden_parada",
        "latitud",
        "longitud",
        "login_usuario",
    ];

    private static int _contador = 500_000_000;

    private readonly HttpClient _client;

    public CarguesApiTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Cargue_ProcesaExcelYExponeFilasConObservaciones()
    {
        await AutenticarComoSemillaAsync();
        var contexto = await PrepararConductorAptoAsync();

        var codigoProcesado = CodigoUnico();
        var codigoRechazado = CodigoUnico();

        var excel = CrearExcel(
            Encabezados,
            [
                codigoProcesado, contexto.NumeroIdentificacion, contexto.Placa,
                "Terminal Norte", "0", "6.2442", "-75.5812", ApiFactory.SeedLogin,
            ],
            [
                codigoProcesado, contexto.NumeroIdentificacion, contexto.Placa,
                "Parque Principal", "1", string.Empty, string.Empty, ApiFactory.SeedLogin,
            ],
            [
                codigoRechazado, "999999999", contexto.Placa,
                "Origen inexistente", "0", string.Empty, string.Empty, ApiFactory.SeedLogin,
            ]);

        var cargue = await SubirAsync(excel, "trayectos.xlsx");

        cargue.StatusCode.Should().Be(HttpStatusCode.Created);
        cargue.Headers.Location!.ToString().Should().Contain("/api/cargues/");

        var resumen = await ReadAs<CargueResumenDto>(cargue);
        resumen.IdCargue.Should().BeGreaterThan(0);
        resumen.Total.Should().Be(3);
        resumen.Procesados.Should().Be(2);
        resumen.Errores.Should().Be(1);

        var detalle = await _client.GetAsync($"/api/cargues/{resumen.IdCargue}");
        detalle.StatusCode.Should().Be(HttpStatusCode.OK);
        var filas = await ReadAs<CargueDetalleDto>(detalle);
        filas.Filas.Should().HaveCount(3);

        var rechazada = filas.Filas.Single(f => f.CodigoRuta == codigoRechazado);
        rechazada.Estado.Should().Be(EstadoTrayectoTemporal.Error);
        rechazada.Observacion.Should().Contain("conductor no existe");

        var procesadas = filas.Filas.Where(f => f.CodigoRuta == codigoProcesado).ToList();
        procesadas.Should().HaveCount(2);
        procesadas.Should().OnlyContain(f => f.Estado == EstadoTrayectoTemporal.Procesado);
        procesadas.Select(f => f.OrdenParada).Should().BeEquivalentTo(["0", "1"]);

        var soloErrores = await ReadAs<CargueDetalleDto>(
            await _client.GetAsync($"/api/cargues/{resumen.IdCargue}?estado=ERROR"));
        soloErrores.Filas.Should().ContainSingle().Which.CodigoRuta.Should().Be(codigoRechazado);

        var listadoRespuesta = await _client.GetAsync("/api/cargues");
        listadoRespuesta.StatusCode.Should().Be(
            HttpStatusCode.OK, await listadoRespuesta.Content.ReadAsStringAsync());
        var listado = await ReadAs<List<CargueResumenDto>>(listadoRespuesta);
        listado.Should().Contain(c => c.IdCargue == resumen.IdCargue);

        var ruta = await _client.GetAsync($"/api/rutas/{codigoProcesado}");
        ruta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAs<RutaProcesadaDto>(ruta)).Paradas.Should().HaveCount(2);

        (await _client.DeleteAsync($"/api/cargues/{resumen.IdCargue}")).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        var eliminado = await _client.GetAsync($"/api/cargues/{resumen.IdCargue}");
        eliminado.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await eliminado.Content.ReadAsStringAsync()).Should().Contain("cargue.not_found");

        // La ruta ya procesada no se borra al purgar la staging del cargue.
        (await _client.GetAsync($"/api/rutas/{codigoProcesado}")).StatusCode
            .Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Cargue_RechazaArchivoSinColumnasObligatorias()
    {
        await AutenticarComoSemillaAsync();

        var excel = CrearExcel(["codigo_ruta", "ubicacion"], ["RUTA-X", "Origen"]);
        var respuesta = await SubirAsync(excel, "incompleto.xlsx");

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await respuesta.Content.ReadAsStringAsync()).Should().Contain("cargue.archivo.formato");
    }

    [Fact]
    public async Task Cargue_RechazaFilasEstructuralmenteInvalidas()
    {
        await AutenticarComoSemillaAsync();

        var excel = CrearExcel(
            Encabezados,
            [CodigoUnico(), "123456789", "RTZ123", "Origen", "9", string.Empty, string.Empty, ApiFactory.SeedLogin]);

        var respuesta = await SubirAsync(excel, "invalido.xlsx");

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await respuesta.Content.ReadAsStringAsync()).Should().Contain("cargue.columnas.invalidas");
    }

    private sealed record ContextoRuta(string NumeroIdentificacion, string Placa);

    private async Task<ContextoRuta> PrepararConductorAptoAsync()
    {
        var tipoDocumentoId = await RegistrarTipoDocumentoAsync();
        var placa = PlacaUnica();
        var vehiculo = await CrearVehiculoAsync(placa, tipoDocumentoId);
        await HabilitarDocumentoAsync(vehiculo.Id, tipoDocumentoId);

        var numero = NumeroUnico();
        var conductor = await CrearPersonaAsync(numero);

        var licencia = await _client.PutAsync($"/api/personas/{conductor.Id}/licencia", Json(new
        {
            contenidoBase64 = Convert.ToBase64String("%PDF-1.4 licencia cargue"u8.ToArray()),
            fechaVigencia = DateTime.UtcNow.AddYears(1)
        }));
        licencia.EnsureSuccessStatusCode();

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

        return new ContextoRuta(numero, placa);
    }

    private async Task<HttpResponseMessage> SubirAsync(byte[] contenido, string nombreArchivo)
    {
        using var form = new MultipartFormDataContent();
        var archivo = new ByteArrayContent(contenido);
        archivo.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        form.Add(archivo, "archivo", nombreArchivo);

        return await _client.PostAsync("/api/cargues", form);
    }

    private static byte[] CrearExcel(string[] encabezados, params string[][] filas)
    {
        using var workbook = new XLWorkbook();
        var hoja = workbook.Worksheets.Add("Trayectos");

        for (var columna = 0; columna < encabezados.Length; columna++)
        {
            hoja.Cell(1, columna + 1).Value = encabezados[columna];
        }

        for (var fila = 0; fila < filas.Length; fila++)
        {
            for (var columna = 0; columna < filas[fila].Length; columna++)
            {
                hoja.Cell(fila + 2, columna + 1).Value = filas[fila][columna];
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
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

    private async Task<PersonaCreadaDto> CrearPersonaAsync(string numero)
    {
        var response = await _client.PostAsync("/api/personas", Json(new
        {
            tipoIdentificacion = 1,
            numeroIdentificacion = numero,
            nombres = "Cargue",
            apellidos = "Prueba",
            correoElectronico = $"cargue.{numero}@example.com",
            tipoPersona = 2
        }));

        response.EnsureSuccessStatusCode();
        return await ReadAs<PersonaCreadaDto>(response);
    }

    private async Task<Guid> RegistrarTipoDocumentoAsync()
    {
        var codigo = $"CT{NumeroCorto()}";

        var response = await _client.PostAsync("/api/tipos-documento", Json(new
        {
            codigo,
            nombre = $"TipoDocumento {codigo}",
            tiposVehiculoAplicables = "A",
            codigoObligatoriedad = "RA",
            descripcion = "TipoDocumento para pruebas de cargue"
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
            color = "#00FF00",
            modelo = 2020,
            marca = "Toyota",
            linea = "Corolla",
            tipoDocumentoId,
            documentoBase64 = Convert.ToBase64String("%PDF-1.4 cargue"u8.ToArray()),
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

    private static string PlacaUnica() => "CTZ" + Interlocked.Increment(ref _contador).ToString()[^3..];

    private static string CodigoUnico() => $"CAR{Interlocked.Increment(ref _contador)}";

    private static StringContent Json(object body)
        => new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private static async Task<T> ReadAs<T>(HttpResponseMessage response)
        => JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(), JsonOptions)!;

    private sealed record TokenDto(string? AccessToken, string? AccessTokenType, string? ApiKey);

    private sealed record PersonaCreadaDto(Guid Id);

    private sealed record TipoDocumentoDto(Guid Id);

    private sealed record VehiculoCreadoDto(Guid Id);

    private sealed record CargueResumenDto(
        long IdCargue,
        int Total,
        int Cargados,
        int Validados,
        int Procesados,
        int Errores);

    private sealed record CargueDetalleDto(long IdCargue, List<TrayectoFilaDto> Filas);

    private sealed record TrayectoFilaDto(
        Guid Id,
        string? CodigoRuta,
        string? OrdenParada,
        EstadoTrayectoTemporal Estado,
        string? Observacion);

    private sealed record RutaProcesadaDto(Guid Id, string CodigoRuta, List<ParadaProcesadaDto> Paradas);

    private sealed record ParadaProcesadaDto(Guid Id, string Ubicacion, int OrdenParada);
}
