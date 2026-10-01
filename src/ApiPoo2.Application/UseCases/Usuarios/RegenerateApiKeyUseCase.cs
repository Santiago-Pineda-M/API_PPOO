using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Users;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Usuarios;

public sealed record RegenerateApiKeyInputDto(string Login);

public sealed record RegenerateApiKeyOutputDto(string Login, string ApiKey);

/// <summary>
///     El enunciado pide un servicio GET que vuelva a generar la APIKey de un usuario.
/// </summary>
public sealed class RegenerateApiKeyUseCase : BaseUseCase<RegenerateApiKeyInputDto, RegenerateApiKeyOutputDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RegenerateApiKeyUseCase(
        IEnumerable<IValidator<RegenerateApiKeyInputDto>> validators,
        ILoggerFactory loggerFactory,
        IUserRepository userRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _userRepository = userRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<RegenerateApiKeyOutputDto> ExecuteCoreAsync(
        RegenerateApiKeyInputDto request,
        CancellationToken cancellationToken)
    {
        var login = Login.From(request.Login);

        var user = await _userRepository.GetByLoginAsync(login, cancellationToken)
            ?? throw new NotFoundException("user.not_found", "El usuario no existe.");

        user.RegenerarApiKey(_dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegenerateApiKeyOutputDto(user.Login.Value, user.ApiKey.Value);
    }
}
