using ApiPoo2.Domain.Personas;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Personas.Licencia;

public sealed record GetLicenciaConductorInputDto(Guid PersonaId);

public sealed record GetLicenciaConductorOutputDto(
    Guid PersonaId,
    string NumeroIdentificacion,
    string Nombres,
    string Apellidos,
    DateTime FechaVigencia,
    bool Vigente,
    int LongitudBytes)
{
    public static GetLicenciaConductorOutputDto From(Persona persona, DateTime ahora) => new(
        persona.Id,
        persona.NumeroIdentificacion.Value,
        persona.Nombres.Value,
        persona.Apellidos.Value,
        persona.FechaVigenciaLicencia ?? DateTime.MinValue,
        persona.LicenciaVigente(ahora),
        persona.Licencia?.Length ?? 0);
}

public sealed class GetLicenciaConductorUseCase
    : BaseUseCase<GetLicenciaConductorInputDto, GetLicenciaConductorOutputDto>
{
    private readonly IPersonaRepository _personaRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetLicenciaConductorUseCase(
        IEnumerable<FluentValidation.IValidator<GetLicenciaConductorInputDto>> validators,
        ILoggerFactory loggerFactory,
        IPersonaRepository personaRepository,
        IDateTimeProvider dateTimeProvider)
        : base(validators, loggerFactory)
    {
        _personaRepository = personaRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<GetLicenciaConductorOutputDto> ExecuteCoreAsync(
        GetLicenciaConductorInputDto request,
        CancellationToken cancellationToken)
    {
        var persona = await _personaRepository.GetByIdAsync(request.PersonaId, cancellationToken)
            ?? throw new NotFoundException("person.not_found", "La persona no existe.");

        if (!persona.TieneLicencia())
        {
            throw new NotFoundException(
                "persona.licencia.not_found",
                "La persona no tiene una licencia de conducción registrada.");
        }

        return GetLicenciaConductorOutputDto.From(persona, _dateTimeProvider.UtcNow);
    }
}
