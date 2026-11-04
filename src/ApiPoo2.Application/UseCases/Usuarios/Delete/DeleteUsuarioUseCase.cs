using ApiPoo2.Application.UseCases.Auth;
using ApiPoo2.Application.UseCases.Usuarios.Delete;
using ApiPoo2.Domain.Personas;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Usuarios.Delete;

public sealed class DeleteUsuarioUseCase : BaseUseCase<DeleteUsuarioInputDto, OperationResult>
{
    private readonly IPersonaRepository _personaRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteUsuarioUseCase(
        IEnumerable<IValidator<DeleteUsuarioInputDto>> validators,
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
        DeleteUsuarioInputDto request,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var persona = await _personaRepository.GetByIdAsync(request.PersonaId, cancellationToken)
            ?? throw new NotFoundException("persona.not_found", "La persona no existe.");

        var usuario = persona.Usuario
            ?? throw new NotFoundException("usuario.no_encontrado", "La persona no tiene un usuario asociado.");

        // Eliminación lógica: se desactiva el usuario y se revocan sus sesiones activas.
        usuario.Deactivate(now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }
}
