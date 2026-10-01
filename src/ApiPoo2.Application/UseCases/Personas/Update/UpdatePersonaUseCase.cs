using ApiPoo2.Application.UseCases.Personas.Update;
using ApiPoo2.Domain.Personas;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Personas.Update;

public sealed class UpdatePersonaUseCase : BaseUseCase<UpdatePersonaInputDto, UpdatePersonaOutputDto>
{
    private readonly IPersonaRepository _personaRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePersonaUseCase(
        IEnumerable<IValidator<UpdatePersonaInputDto>> validators,
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

    protected override async Task<UpdatePersonaOutputDto> ExecuteCoreAsync(
        UpdatePersonaInputDto request,
        CancellationToken cancellationToken)
    {
        var persona = await _personaRepository.GetByIdAsync(request.PersonaId, cancellationToken)
            ?? throw new NotFoundException("person.not_found", "La persona no existe.");

        var correo = CorreoElectronico.From(request.CorreoElectronico);
        var existente = await _personaRepository.GetByCorreoAsync(correo, cancellationToken);
        if (existente is not null && existente.Id != persona.Id)
        {
            throw new ConflictException(
                "persona.correo.conflict",
                "Ya existe otra persona registrada con ese correo electrónico.");
        }

        persona.ActualizarDatos(request.Nombres, request.Apellidos, correo.Value, _dateTimeProvider.UtcNow);
        persona.CambiarTipoPersona(request.TipoPersona, _dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UpdatePersonaOutputDto.From(persona);
    }
}
