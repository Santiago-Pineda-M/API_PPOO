using ApiPoo2.Application.UseCases.Auth;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Personas.Licencia;

public sealed record EliminarLicenciaConductorInputDto(Guid PersonaId);

public sealed class EliminarLicenciaConductorUseCase
    : BaseUseCase<EliminarLicenciaConductorInputDto, OperationResult>
{
    private readonly IPersonaRepository _personaRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public EliminarLicenciaConductorUseCase(
        IEnumerable<FluentValidation.IValidator<EliminarLicenciaConductorInputDto>> validators,
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

    protected override async Task<OperationResult> ExecuteCoreAsync(
        EliminarLicenciaConductorInputDto request,
        CancellationToken cancellationToken)
    {
        var persona = await _personaRepository.GetByIdAsync(request.PersonaId, cancellationToken)
            ?? throw new NotFoundException("person.not_found", "La persona no existe.");

        persona.EliminarLicencia(_dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }
}
