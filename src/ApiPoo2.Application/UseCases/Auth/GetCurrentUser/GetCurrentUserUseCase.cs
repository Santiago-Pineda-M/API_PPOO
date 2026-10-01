using ApiPoo2.Application.UseCases.Auth;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Auth;

public sealed class GetCurrentUserUseCase : BaseUseCase<GetCurrentUserInputDto, CurrentUserOutputDto>
{
    private readonly IUserRepository _userRepository;

    public GetCurrentUserUseCase(
        IEnumerable<IValidator<GetCurrentUserInputDto>> validators,
        ILoggerFactory loggerFactory,
        IUserRepository userRepository)
        : base(validators, loggerFactory)
    {
        _userRepository = userRepository;
    }

    protected override async Task<CurrentUserOutputDto> ExecuteCoreAsync(GetCurrentUserInputDto request, CancellationToken cancellationToken)
    {
        var persona = await _userRepository.GetPersonaByIdAsync(request.PersonaId, cancellationToken)
            ?? throw new NotFoundException("person.not_found", "La persona no existe.");

        return CurrentUserOutputDto.From(persona);
    }
}
