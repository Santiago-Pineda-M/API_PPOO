using ApiPoo2.Application.UseCases.Auth;
using ApiPoo2.Application.UseCases.Personas.Create;

using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Users;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Usuarios;

/// <summary>
///     El enunciado pide: la nueva contraseña viaja en el body y el login del usuario en la URL.
///     El controller arma el DTO con el valor de la ruta.
/// </summary>
public sealed record ChangeUserPasswordInputDto(string Login, string NewPassword);

public sealed class ChangeUserPasswordUseCase : BaseUseCase<ChangeUserPasswordInputDto, OperationResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeUserPasswordUseCase(
        IEnumerable<IValidator<ChangeUserPasswordInputDto>> validators,
        ILoggerFactory loggerFactory,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<OperationResult> ExecuteCoreAsync(
        ChangeUserPasswordInputDto request,
        CancellationToken cancellationToken)
    {
        var login = Login.From(request.Login);

        var user = await _userRepository.GetByLoginAsync(login, cancellationToken)
            ?? throw new NotFoundException("user.not_found", "El usuario no existe.");

        user.ChangePassword(_passwordHasher.Hash(request.NewPassword), _dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }
}
