using FluentValidation;
using ApiPoo2.Domain.Personas;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Rutas.PorConductor;

/// <summary>
///     Consulta del enunciado: dada la identificación de un conductor se devuelven únicamente los
///     códigos de sus rutas, agrupados (sin repetir) y ordenados.
/// </summary>
public sealed record ListRutasPorConductorInputDto(string NumeroIdentificacion);

public sealed class ListRutasPorConductorValidator : AbstractValidator<ListRutasPorConductorInputDto>
{
    public ListRutasPorConductorValidator()
    {
        RuleFor(x => x.NumeroIdentificacion)
            .NotEmpty().WithMessage("El número de identificación del conductor es obligatorio.")
            .MaximumLength(20).WithMessage("El número de identificación no puede superar los 20 caracteres.");
    }
}

public sealed class ListRutasPorConductorUseCase
    : BaseUseCase<ListRutasPorConductorInputDto, IReadOnlyList<string>>
{
    private readonly IPersonaRepository _personaRepository;
    private readonly IRutaRepository _rutaRepository;

    public ListRutasPorConductorUseCase(
        IEnumerable<IValidator<ListRutasPorConductorInputDto>> validators,
        ILoggerFactory loggerFactory,
        IPersonaRepository personaRepository,
        IRutaRepository rutaRepository)
        : base(validators, loggerFactory)
    {
        _personaRepository = personaRepository;
        _rutaRepository = rutaRepository;
    }

    protected override async Task<IReadOnlyList<string>> ExecuteCoreAsync(
        ListRutasPorConductorInputDto request,
        CancellationToken cancellationToken)
    {
        var numero = NumeroIdentificacion.From(request.NumeroIdentificacion);
        var persona = await _personaRepository.GetByNumeroIdentificacionAsync(numero, cancellationToken);

        if (persona is null)
        {
            return [];
        }

        var rutas = await _rutaRepository.GetByPersonaAsync(persona.Id, cancellationToken);

        return rutas
            .Select(r => r.Codigo.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
