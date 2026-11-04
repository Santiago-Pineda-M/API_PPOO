namespace ApiPoo2.Application.IServices;

/// <summary>Coordenadas geográficas de una parada.</summary>
public sealed record Coordenadas(double Latitud, double Longitud);

/// <summary>
///     Puerto de geocodificación. La implementación concreta vive en Infrastructure para que el
///     proveedor (Nominatim, Photon, etc.) se pueda cambiar sin tocar casos de uso ni dominio.
/// </summary>
public interface IGeocodingService
{
    Task<Coordenadas?> GeocodeAsync(string ubicacion, CancellationToken cancellationToken = default);
}
