using ApiPoo2.Application.UseCases.Usuarios.Get;

using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Usuarios.Get;

public sealed class GetUsuarioUseCase : BaseUseCase<GetUsuarioInputDto, UsuarioOutputDto>
{
    private readonly IPersonaRepository _personaRepository;

    public GetUsuarioUseCase(
        IEnumerable<IValidator<GetUsuarioInputDto>> validators,
        ILoggerFactory loggerFactory,
        IPersonaRepository personaRepository)
        : base(validators, loggerFactory)
    {
        _personaRepository = personaRepository;
    }

    protected override async Task<UsuarioOutputDto> ExecuteCoreAsync(
        GetUsuarioInputDto request,
        CancellationToken cancellationToken)
    {
        var persona = await _personaRepository.GetByIdAsync(request.PersonaId, cancellationToken)
            ?? throw new NotFoundException("persona.not_found", "La persona no existe.");

        return UsuarioOutputDto.From(persona);
    }
}
