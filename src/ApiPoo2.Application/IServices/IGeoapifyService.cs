namespace ApiPoo2.Application.IServices;

public sealed record GeoapifyAddress(
    string Formatted,
    string? Name,
    string? City,
    string? Country,
    double Latitude,
    double Longitude);

public sealed record GeoapifyCoordinate(double Latitude, double Longitude);

public sealed record GeoapifyRoute(double DistanceMeters, double DurationSeconds, IReadOnlyList<GeoapifyCoordinate> Coordinates);

public interface IGeoapifyService
{
    Task<IReadOnlyList<GeoapifyAddress>> SearchAddressesAsync(
        string query,
        int limit,
        CancellationToken cancellationToken);

    Task<GeoapifyRoute> CalculateRouteAsync(
        GeoapifyCoordinate origin,
        GeoapifyCoordinate destination,
        string mode,
        CancellationToken cancellationToken);
}
