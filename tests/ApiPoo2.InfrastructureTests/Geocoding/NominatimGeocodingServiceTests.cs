using ApiPoo2.Application.IServices;
using ApiPoo2.Infrastructure.Geocoding;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiPoo2.InfrastructureTests.Geocoding;

public sealed class NominatimGeocodingServiceTests
{
    private static readonly GeocodingOptions Opciones = new()
    {
        BaseUrl = "https://nominatim.test",
        UserAgent = "API_PPOO/1.0 (test@example.com)",
        PaisCodigo = "co",
        TimeoutSegundos = 5,
        PausaEntreLlamadasMs = 0,
        MaxPorCiclo = 10,
        CacheTtlHoras = 24,
    };

    [Fact]
    public async Task Geocode_DevuelveCoordenadasYRespetaElContratoDeNominatim()
    {
        var handler = new HandlerFalso(_ => Json("""[{"lat":"4.4389","lon":"-75.2322"}]"""));
        var servicio = CrearServicio(handler);

        var coordenadas = await servicio.GeocodeAsync("Terminal Norte");

        coordenadas.Should().Be(new Coordenadas(4.4389, -75.2322));

        var solicitud = handler.Solicitudes.Single();
        solicitud.Url.Should().Contain("search?");
        solicitud.Url.Should().Contain("q=Terminal%20Norte");
        solicitud.Url.Should().Contain("format=jsonv2");
        solicitud.Url.Should().Contain("countrycodes=co");
        solicitud.UserAgent.Should().Contain("API_PPOO/1.0");
        solicitud.UserAgent.Should().Contain("test@example.com");
        solicitud.AcceptLanguage.Should().Be("es");
    }

    [Fact]
    public async Task Geocode_DevuelveNuloCuandoNominatimNoEncuentraResultados()
    {
        var servicio = CrearServicio(new HandlerFalso(_ => Json("[]")));

        var coordenadas = await servicio.GeocodeAsync("Lugar inexistente");

        coordenadas.Should().BeNull();
    }

    [Fact]
    public async Task Geocode_DevuelveNuloCuandoLaRespuestaEstaMalFormada()
    {
        var servicio = CrearServicio(new HandlerFalso(_ => Json("{ esto no es json")));

        var coordenadas = await servicio.GeocodeAsync("Terminal Norte");

        coordenadas.Should().BeNull();
    }

    [Fact]
    public async Task Geocode_DevuelveNuloCuandoNominatimLimitaLaTasa()
    {
        var servicio = CrearServicio(new HandlerFalso(_ =>
            new HttpResponseMessage((System.Net.HttpStatusCode)429)));

        var coordenadas = await servicio.GeocodeAsync("Terminal Norte");

        coordenadas.Should().BeNull();
    }

    [Fact]
    public async Task Geocode_DevuelveNuloCuandoLasCoordenadasEstanFueraDeRango()
    {
        var servicio = CrearServicio(new HandlerFalso(_ => Json("""[{"lat":"200","lon":"-75.2322"}]""")));

        var coordenadas = await servicio.GeocodeAsync("Terminal Norte");

        coordenadas.Should().BeNull();
    }

    [Fact]
    public async Task Geocode_ReutilizaLaCacheParaLaMismaUbicacion()
    {
        var handler = new HandlerFalso(_ => Json("""[{"lat":"4.4389","lon":"-75.2322"}]"""));
        var servicio = CrearServicio(handler);

        await servicio.GeocodeAsync("Terminal Norte");
        await servicio.GeocodeAsync(" terminal norte ");

        handler.Solicitudes.Should().HaveCount(1);
    }

    [Fact]
    public async Task Geocode_DevuelveNuloSinLlamarAlServicioCuandoLaUbicacionEstaVacia()
    {
        var handler = new HandlerFalso(_ => Json("[]"));
        var servicio = CrearServicio(handler);

        var coordenadas = await servicio.GeocodeAsync("   ");

        coordenadas.Should().BeNull();
        handler.Solicitudes.Should().BeEmpty();
    }

    private static NominatimGeocodingService CrearServicio(HttpMessageHandler handler)
        => new(
            new HttpClient(handler),
            new MemoryCache(new MemoryCacheOptions()),
            Opciones,
            NullLogger<NominatimGeocodingService>.Instance);

    private static HttpResponseMessage Json(string contenido)
        => new(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(contenido, System.Text.Encoding.UTF8, "application/json"),
        };

    private sealed class HandlerFalso(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        public List<Solicitud> Solicitudes { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Solicitudes.Add(new Solicitud(
                request.RequestUri!.AbsoluteUri,
                request.Headers.TryGetValues("User-Agent", out var userAgent)
                    ? string.Join(",", userAgent)
                    : null,
                request.Headers.TryGetValues("Accept-Language", out var idiomas)
                    ? string.Join(",", idiomas)
                    : null));

            return Task.FromResult(responder(request));
        }
    }

    private sealed record Solicitud(string Url, string? UserAgent, string? AcceptLanguage);
}
