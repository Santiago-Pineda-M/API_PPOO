using ApiPoo2.Application.IServices;
using ApiPoo2.Application.UseCases;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Mapas.CalcularRuta;

public sealed record CalcularRutaInputDto(
    double OrigenLatitud,
    double OrigenLongitud,
    double DestinoLatitud,
    double DestinoLongitud,
    string Modo = "drive");

public sealed record CalcularRutaOutputDto(
    double DistanciaMetros,
    double DuracionSegundos,
    IReadOnlyList<GeoapifyCoordinate> Coordenadas);

public sealed class CalcularRutaUseCase
    : BaseUseCase<CalcularRutaInputDto, CalcularRutaOutputDto>
{
    private readonly IGeoapifyService _geoapifyService;

    public CalcularRutaUseCase(
        IEnumerable<IValidator<CalcularRutaInputDto>> validators,
        ILoggerFactory loggerFactory,
        IGeoapifyService geoapifyService)
        : base(validators, loggerFactory)
    {
        _geoapifyService = geoapifyService;
    }

    protected override async Task<CalcularRutaOutputDto> ExecuteCoreAsync(
        CalcularRutaInputDto request,
        CancellationToken cancellationToken)
    {
        var route = await _geoapifyService.CalculateRouteAsync(
            new GeoapifyCoordinate(request.OrigenLatitud, request.OrigenLongitud),
            new GeoapifyCoordinate(request.DestinoLatitud, request.DestinoLongitud),
            request.Modo,
            cancellationToken);

        return new CalcularRutaOutputDto(
            route.DistanceMeters,
            route.DurationSeconds,
            route.Coordinates);
    }
}

public sealed class CalcularRutaValidator : AbstractValidator<CalcularRutaInputDto>
{
    private static readonly string[] SupportedModes =
    [
        "drive", "light_truck", "medium_truck", "truck", "heavy_truck", "long_truck",
        "truck_dangerous_goods", "bus", "scooter", "motorcycle", "bicycle",
        "mountain_bike", "road_bike", "walk", "hike", "transit", "approximated_transit",
    ];

    public CalcularRutaValidator()
    {
        RuleFor(request => request.OrigenLatitud)
            .InclusiveBetween(-90, 90).WithMessage("La latitud de origen debe estar entre -90 y 90.");
        RuleFor(request => request.OrigenLongitud)
            .InclusiveBetween(-180, 180).WithMessage("La longitud de origen debe estar entre -180 y 180.");
        RuleFor(request => request.DestinoLatitud)
            .InclusiveBetween(-90, 90).WithMessage("La latitud de destino debe estar entre -90 y 90.");
        RuleFor(request => request.DestinoLongitud)
            .InclusiveBetween(-180, 180).WithMessage("La longitud de destino debe estar entre -180 y 180.");
        RuleFor(request => request.Modo)
            .Must(mode => SupportedModes.Contains(mode, StringComparer.Ordinal))
            .WithMessage("El modo de transporte no es válido.");
    }
}
