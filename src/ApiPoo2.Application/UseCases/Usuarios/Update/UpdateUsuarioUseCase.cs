using ApiPoo2.Application.UseCases.Usuarios.Update;
using ApiPoo2.Domain.Personas;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Usuarios.Update;

public sealed class UpdateUsuarioUseCase : BaseUseCase<UpdateUsuarioInputDto, UsuarioOutputDto>
{
    private readonly IPersonaRepository _personaRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUsuarioUseCase(
        IEnumerable<IValidator<UpdateUsuarioInputDto>> validators,
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
        UpdateUsuarioInputDto request,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var persona = await _personaRepository.GetByIdAsync(request.PersonaId, cancellationToken)
            ?? throw new NotFoundException("persona.not_found", "La persona no existe.");

        var usuario = persona.Usuario
            ?? throw new NotFoundException("usuario.no_encontrado", "La persona no tiene un usuario asociado.");

        if (!string.IsNullOrWhiteSpace(request.Nombres)
            || !string.IsNullOrWhiteSpace(request.Apellidos)
            || !string.IsNullOrWhiteSpace(request.CorreoElectronico))
        {
            var nombres = request.Nombres ?? persona.Nombres.Value;
            var apellidos = request.Apellidos ?? persona.Apellidos.Value;
            var correo = request.CorreoElectronico ?? persona.CorreoElectronico.Value;

            var correoValido = CorreoElectronico.From(correo);
            var existente = await _personaRepository.GetByCorreoAsync(correoValido, cancellationToken);
            if (existente is not null && existente.Id != persona.Id)
            {
                throw new ConflictException(
                    "persona.correo.conflict",
                    "Ya existe otra persona registrada con ese correo electrónico.");
            }

            persona.ActualizarDatos(nombres, apellidos, correoValido.Value, now);
        }

        if (request.TipoPersona is { } tipoPersona)
        {
            persona.CambiarTipoPersona(tipoPersona, now);
        }

        if (request.IsActive is { } isActive)
        {
            if (isActive)
            {
                usuario.Activate(now);
            }
            else
            {
                usuario.Deactivate(now);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UsuarioOutputDto.From(persona);
    }
}
