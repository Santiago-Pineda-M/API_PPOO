namespace ApiPoo2.Application.IServices;

public sealed class GeocodingOptions
{
    public const string SectionName = "Geocoding";

    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";

    public string UserAgent { get; set; } = "API_PPOO/1.0 (sin-contacto@example.com)";

    public string PaisCodigo { get; set; } = "co";

    public int TimeoutSegundos { get; set; } = 10;

    public int PausaEntreLlamadasMs { get; set; } = 1100;

    public int MaxPorCiclo { get; set; } = 10;

    public int CacheTtlHoras { get; set; } = 24;
}
