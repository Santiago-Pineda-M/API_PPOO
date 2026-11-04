using ApiPoo2.Domain.Rutas;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Rutas.List;

/// <summary>
///     Filtros de consulta del enunciado: por código, por placa del vehículo, por identificación
///     del conductor y por operabilidad.
/// </summary>
public sealed record ListRutasInputDto(
    string? Codigo = null,
    string? Placa = null,
    string? NumeroIdentificacion = null,
    bool? Operable = null);

public sealed class ListRutasUseCase
    : BaseUseCase<ListRutasInputDto, IReadOnlyList<RutaResumenOutputDto>>
{
    private readonly IRutaRepository _rutaRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ListRutasUseCase(
        IEnumerable<FluentValidation.IValidator<ListRutasInputDto>> validators,
        ILoggerFactory loggerFactory,
        IRutaRepository rutaRepository,
        IDateTimeProvider dateTimeProvider)
        : base(validators, loggerFactory)
    {
        _rutaRepository = rutaRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<IReadOnlyList<RutaResumenOutputDto>> ExecuteCoreAsync(
        ListRutasInputDto request,
        CancellationToken cancellationToken)
    {
        var ahora = _dateTimeProvider.UtcNow;
        var rutas = await _rutaRepository.GetAllAsync(cancellationToken);
        IEnumerable<Ruta> filtradas = rutas;

        if (!string.IsNullOrWhiteSpace(request.Codigo))
        {
            var codigo = request.Codigo.Trim();
            filtradas = filtradas.Where(r =>
                r.Codigo.Value.Contains(codigo, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Placa))
        {
            var placa = request.Placa.Trim().ToUpperInvariant();
            filtradas = filtradas.Where(r =>
                string.Equals(r.Vehiculo?.Placa.Value, placa, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.NumeroIdentificacion))
        {
            var numero = request.NumeroIdentificacion.Trim();
            filtradas = filtradas.Where(r =>
                string.Equals(
                    r.Persona?.NumeroIdentificacion.Value,
                    numero,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (request.Operable.HasValue)
        {
            filtradas = filtradas.Where(r =>
                (RutaOperabilidad.MotivoNoOperable(r.VehiculoId, r.Persona, r.Vehiculo, ahora) is null)
                == request.Operable.Value);
        }

        return filtradas
            .OrderBy(r => r.Codigo.Value, StringComparer.OrdinalIgnoreCase)
            .Select(r => RutaResumenOutputDto.From(r, ahora))
            .ToList();
    }
}
