using System.Globalization;
using System.Net;
using System.Text.Json;
using ApiPoo2.Application.IServices;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Infrastructure.Geocoding;

/// <summary>
///     Adaptador de geocodificación sobre OpenStreetMap Nominatim. Respeta la política de uso:
///     User-Agent identificable, una petición por segundo (la pausa la aplica el job), sin cargas
///     masivas y con caché en memoria por ubicación.
/// </summary>
public sealed class NominatimGeocodingService : IGeocodingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly GeocodingOptions _options;
    private readonly ILogger<NominatimGeocodingService> _logger;

    public NominatimGeocodingService(
        HttpClient httpClient,
        IMemoryCache cache,
        GeocodingOptions options,
        ILogger<NominatimGeocodingService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options;
        _logger = logger;

        _httpClient.BaseAddress ??= new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSegundos));
    }

    public async Task<Coordenadas?> GeocodeAsync(
        string ubicacion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ubicacion))
        {
            return null;
        }

        var clave = $"geocoding:{ubicacion.Trim().ToLowerInvariant()}";

        if (_cache.TryGetValue(clave, out Coordenadas? cacheada))
        {
            return cacheada;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSegundos)));

        try
        {
            var respuesta = await EnviarAsync(ubicacion, cts.Token);

            if (respuesta is null || respuesta.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return null;
            }

            if (!respuesta.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Nominatim respondió {Codigo} al geocodificar '{Ubicacion}'.",
                    (int)respuesta.StatusCode,
                    ubicacion);
                return null;
            }

            var coordenadas = await LeerRespuestaAsync(respuesta, ubicacion, cts.Token);

            if (coordenadas is not null)
            {
                _cache.Set(clave, coordenadas, TimeSpan.FromHours(Math.Max(1, _options.CacheTtlHoras)));
            }

            return coordenadas;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("La geocodificación de '{Ubicacion}' superó el tiempo de espera.", ubicacion);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Falló la petición de geocodificación para '{Ubicacion}'.", ubicacion);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Nominatim devolvió una respuesta ilegible para '{Ubicacion}'.", ubicacion);
            return null;
        }
    }

    private async Task<HttpResponseMessage?> EnviarAsync(string ubicacion, CancellationToken cancellationToken)
    {
        var respuesta = await EnviarUnaVezAsync(ubicacion, cancellationToken);

        if (respuesta is not null && (int)respuesta.StatusCode >= 500)
        {
            respuesta.Dispose();
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            respuesta = await EnviarUnaVezAsync(ubicacion, cancellationToken);
        }

        return respuesta;
    }

    private async Task<HttpResponseMessage?> EnviarUnaVezAsync(
        string ubicacion,
        CancellationToken cancellationToken)
    {
        var parametros = new Dictionary<string, string>
        {
            ["q"] = ubicacion.Trim(),
            ["format"] = "jsonv2",
            ["limit"] = "1",
            ["addressdetails"] = "0",
        };

        if (!string.IsNullOrWhiteSpace(_options.PaisCodigo))
        {
            parametros["countrycodes"] = _options.PaisCodigo;
        }

        var query = string.Join("&", parametros.Select(p =>
            $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        using var request = new HttpRequestMessage(HttpMethod.Get, $"search?{query}");
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("Accept-Language", "es");

        return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    private async Task<Coordenadas?> LeerRespuestaAsync(
        HttpResponseMessage respuesta,
        string ubicacion,
        CancellationToken cancellationToken)
    {
        await using var stream = await respuesta.Content.ReadAsStreamAsync(cancellationToken);
        using var documento = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (documento.RootElement.ValueKind != JsonValueKind.Array
            || documento.RootElement.GetArrayLength() == 0)
        {
            _logger.LogInformation("Nominatim no encontró coordenadas para '{Ubicacion}'.", ubicacion);
            return null;
        }

        var primero = documento.RootElement[0];

        if (!primero.TryGetProperty("lat", out var latitudElemento)
            || !primero.TryGetProperty("lon", out var longitudElemento)
            || latitudElemento.ValueKind != JsonValueKind.String
            || longitudElemento.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        if (!double.TryParse(
                latitudElemento.GetString(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var latitud)
            || !double.TryParse(
                longitudElemento.GetString(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var longitud))
        {
            return null;
        }

        if (latitud is < -90 or > 90 || longitud is < -180 or > 180)
        {
            return null;
        }

        return new Coordenadas(latitud, longitud);
    }
}
