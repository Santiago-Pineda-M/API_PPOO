using FluentValidation;
using ApiPoo2.Domain.Rutas;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Rutas.PorPlaca;

/// <summary>Código de ruta con el conductor que la opera, tal como pide la consulta por placa.</summary>
public sealed record RutaConductorOutputDto(string CodigoRuta, string Conductor);

/// <summary>Parámetro de la consulta: la placa del vehículo.</summary>
public sealed record ListRutasPorPlacaInputDto(string Placa);

public sealed class ListRutasPorPlacaValidator : AbstractValidator<ListRutasPorPlacaInputDto>
{
    public ListRutasPorPlacaValidator()
    {
        RuleFor(x => x.Placa)
            .NotEmpty().WithMessage("La placa del vehículo es obligatoria.")
            .MaximumLength(10).WithMessage("La placa no puede superar los 10 caracteres.");
    }
}

/// <summary>
///     Consulta del enunciado: dada la placa de un vehículo se devuelven el código de ruta y el
///     conductor asociado, agrupados (sin repetir parejas) y ordenados por código.
/// </summary>
public sealed class ListRutasPorPlacaUseCase
    : BaseUseCase<ListRutasPorPlacaInputDto, IReadOnlyList<RutaConductorOutputDto>>
{
    private readonly IRutaRepository _rutaRepository;

    public ListRutasPorPlacaUseCase(
        IEnumerable<IValidator<ListRutasPorPlacaInputDto>> validators,
        ILoggerFactory loggerFactory,
        IRutaRepository rutaRepository)
        : base(validators, loggerFactory)
        => _rutaRepository = rutaRepository;

    protected override async Task<IReadOnlyList<RutaConductorOutputDto>> ExecuteCoreAsync(
        ListRutasPorPlacaInputDto request,
        CancellationToken cancellationToken)
    {
        var placa = request.Placa.Trim();
        var rutas = await _rutaRepository.GetAllAsync(cancellationToken);

        return rutas
            .Where(r => string.Equals(
                r.Vehiculo?.Placa.Value,
                placa,
                StringComparison.OrdinalIgnoreCase))
            .Select(r => new RutaConductorOutputDto(r.Codigo.Value, NombreConductor(r)))
            .DistinctBy(r => (r.CodigoRuta, r.Conductor))
            .OrderBy(r => r.CodigoRuta, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NombreConductor(Ruta ruta)
        => ruta.Persona is null
            ? string.Empty
            : $"{ruta.Persona.Nombres.Value} {ruta.Persona.Apellidos.Value}".Trim();
}
