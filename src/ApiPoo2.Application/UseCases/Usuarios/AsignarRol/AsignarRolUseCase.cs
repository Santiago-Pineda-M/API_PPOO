using ApiPoo2.Application.UseCases.Usuarios.AsignarRol;
using ApiPoo2.Domain.Personas;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Usuarios.AsignarRol;

public sealed class AsignarRolUseCase : BaseUseCase<AsignarRolInputDto, UsuarioOutputDto>
{
    private readonly IPersonaRepository _personaRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public AsignarRolUseCase(
        IEnumerable<IValidator<AsignarRolInputDto>> validators,
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

    protected override async Task<UsuarioOutputDto> ExecuteCoreAsync(
        AsignarRolInputDto request,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var persona = await _personaRepository.GetByIdAsync(request.PersonaId, cancellationToken)
            ?? throw new NotFoundException("persona.not_found", "La persona no existe.");

        var usuario = persona.Usuario
            ?? throw new NotFoundException("usuario.no_encontrado", "La persona no tiene un usuario asociado.");

        usuario.CambiarRol(request.Rol, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UsuarioOutputDto.From(persona);
    }
}
