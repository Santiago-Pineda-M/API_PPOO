using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Personas.Licencia;

public sealed record RegistrarLicenciaConductorInputDto(
    Guid PersonaId,
    string ContenidoBase64,
    DateTime FechaVigencia);

public sealed class RegistrarLicenciaConductorValidator
    : AbstractValidator<RegistrarLicenciaConductorInputDto>
{
    public RegistrarLicenciaConductorValidator()
    {
        RuleFor(x => x.PersonaId)
            .NotEmpty().WithMessage("La persona es obligatoria.");

        RuleFor(x => x.ContenidoBase64)
            .NotEmpty().WithMessage("El contenido Base64 de la licencia es obligatorio.");

        RuleFor(x => x.FechaVigencia)
            .NotEqual(default(DateTime)).WithMessage("La fecha de vigencia de la licencia es obligatoria.");
    }
}

public sealed class RegistrarLicenciaConductorUseCase
    : BaseUseCase<RegistrarLicenciaConductorInputDto, GetLicenciaConductorOutputDto>
{
    private readonly IPersonaRepository _personaRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RegistrarLicenciaConductorUseCase(
        IEnumerable<IValidator<RegistrarLicenciaConductorInputDto>> validators,
        ILoggerFactory loggerFactory,
        IPersonaRepository personaRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _personaRepository = personaRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<GetLicenciaConductorOutputDto> ExecuteCoreAsync(
        RegistrarLicenciaConductorInputDto request,
        CancellationToken cancellationToken)
    {
        var ahora = _dateTimeProvider.UtcNow;
        var persona = await _personaRepository.GetByIdAsync(request.PersonaId, cancellationToken)
            ?? throw new NotFoundException("person.not_found", "La persona no existe.");

        if (!persona.EsConductor())
        {
            throw new ForbiddenException(
                "persona.licencia.tipo_persona",
                "Solo las personas de tipo CONDUCTOR pueden tener licencia de conducción.");
        }

        byte[] contenido;
        try
        {
            contenido = Convert.FromBase64String(request.ContenidoBase64.Trim());
        }
        catch (FormatException)
        {
            throw new RequestValidationException(
                ["El contenido enviado no es un Base64 válido."]);
        }

        persona.RegistrarLicencia(contenido, request.FechaVigencia, ahora);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return GetLicenciaConductorOutputDto.From(persona, ahora);
    }
}
