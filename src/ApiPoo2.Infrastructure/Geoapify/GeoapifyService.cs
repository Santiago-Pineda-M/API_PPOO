using System.Globalization;
using System.Text.Json;
using ApiPoo2.Application.Exceptions;
using ApiPoo2.Application.IServices;
using Microsoft.Extensions.Configuration;

namespace ApiPoo2.Infrastructure.Geoapify;

public sealed class GeoapifyService : IGeoapifyService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public GeoapifyService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Geoapify__ApiKey"];
    }

    public async Task<IReadOnlyList<GeoapifyAddress>> SearchAddressesAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        var uri = $"v1/geocode/search?text={Uri.EscapeDataString(query)}&limit={limit}&lang=es&apiKey={GetApiKey()}";
        using var document = await GetJsonAsync(uri, cancellationToken);

        var features = GetRequiredProperty(document.RootElement, "features");
        if (features.ValueKind != JsonValueKind.Array)
        {
            throw InvalidResponse();
        }

        return features.EnumerateArray()
            .Select(ParseAddress)
            .ToList();
    }

    public async Task<GeoapifyRoute> CalculateRouteAsync(
        GeoapifyCoordinate origin,
        GeoapifyCoordinate destination,
        string mode,
        CancellationToken cancellationToken)
    {
        var waypoints = string.Create(
            CultureInfo.InvariantCulture,
            $"{origin.Latitude},{origin.Longitude}|{destination.Latitude},{destination.Longitude}");
        var uri = $"v1/routing?waypoints={Uri.EscapeDataString(waypoints)}&mode={Uri.EscapeDataString(mode)}&lang=es&apiKey={GetApiKey()}";
        using var document = await GetJsonAsync(uri, cancellationToken);

        var features = GetRequiredProperty(document.RootElement, "features");
        if (features.ValueKind != JsonValueKind.Array
            || features.GetArrayLength() == 0)
        {
            throw InvalidResponse();
        }

        var feature = features[0];
        var properties = GetRequiredProperty(feature, "properties");
        var geometry = GetRequiredProperty(feature, "geometry");
        var coordinates = GetRequiredProperty(geometry, "coordinates");
        if (coordinates.ValueKind != JsonValueKind.Array)
        {
            throw InvalidResponse();
        }

        var routeCoordinates = new List<GeoapifyCoordinate>();
        foreach (var point in coordinates.EnumerateArray())
        {
            if (point.ValueKind != JsonValueKind.Array
                || point.GetArrayLength() < 2
                || !point[0].TryGetDouble(out var longitude)
                || !point[1].TryGetDouble(out var latitude))
            {
                throw InvalidResponse();
            }

            routeCoordinates.Add(new GeoapifyCoordinate(latitude, longitude));
        }

        return new GeoapifyRoute(
            GetDouble(properties, "distance"),
            GetDouble(properties, "time"),
            routeCoordinates);
    }

    private async Task<JsonDocument> GetJsonAsync(string relativeUri, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(relativeUri, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new ExternalServiceException(
                "geoapify.unavailable",
                "No fue posible comunicarse con el servicio de mapas.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalServiceException(
                    "geoapify.request.failed",
                    "Geoapify no pudo procesar la solicitud.");
            }

            try
            {
                return await JsonDocument.ParseAsync(
                    await response.Content.ReadAsStreamAsync(cancellationToken),
                    cancellationToken: cancellationToken);
            }
            catch (JsonException)
            {
                throw InvalidResponse();
            }
        }
    }

    private string GetApiKey()
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InternalException(
                "geoapify.not_configured",
                "El servicio de mapas no está configurado.");
        }

        return Uri.EscapeDataString(_apiKey);
    }

    private static ExternalServiceException InvalidResponse()
        => new("geoapify.invalid_response", "Geoapify devolvió una respuesta no válida.");

    private static string? GetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static GeoapifyAddress ParseAddress(JsonElement feature)
    {
        var properties = GetRequiredProperty(feature, "properties");
        var formatted = GetString(properties, "formatted") ?? throw InvalidResponse();

        return new GeoapifyAddress(
            formatted,
            GetString(properties, "name"),
            GetString(properties, "city"),
            GetString(properties, "country"),
            GetDouble(properties, "lat"),
            GetDouble(properties, "lon"));
    }

    private static JsonElement GetRequiredProperty(JsonElement element, string propertyName)
        => element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(propertyName, out var value)
                ? value
                : throw InvalidResponse();

    private static double GetDouble(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.TryGetDouble(out var number)
            ? number
            : throw InvalidResponse();
}
