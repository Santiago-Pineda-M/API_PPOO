using ApiPoo2.Application.UseCases.Personas.Get;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Personas.Get;

public sealed class GetPersonaUseCase : BaseUseCase<GetPersonaInputDto, GetPersonaOutputDto>
{
    private readonly IPersonaRepository _personaRepository;

    public GetPersonaUseCase(
        IEnumerable<IValidator<GetPersonaInputDto>> validators,
        ILoggerFactory loggerFactory,
        IPersonaRepository personaRepository)
        : base(validators, loggerFactory)
    {
        _personaRepository = personaRepository;
    }

    protected override async Task<GetPersonaOutputDto> ExecuteCoreAsync(
        GetPersonaInputDto request,
        CancellationToken cancellationToken)
    {
        var persona = await _personaRepository.GetByIdAsync(request.PersonaId, cancellationToken)
            ?? throw new NotFoundException("person.not_found", "La persona no existe.");

        return GetPersonaOutputDto.From(persona);
    }
}
