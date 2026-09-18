using ApiPoo2.Application.Exceptions;
using ApiPoo2.Application.IRepositories;
using ApiPoo2.Application.DTOs;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Auth.GetCurrentUser;

public sealed class GetCurrentUserUseCase : BaseUseCase<GetCurrentUserInputDto, CurrentUserDto>
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

    protected override async Task<CurrentUserDto> ExecuteCoreAsync(GetCurrentUserInputDto request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("user.not_found", "El usuario no existe.");

        return CurrentUserDto.From(user);
    }
}