using ApiPoo2.Application.Exceptions;
using ApiPoo2.Application.Models;
using ApiPoo2.Application.CQRS;
using ApiPoo2.Application.IRepositories;
using ApiPoo2.Application.IServices;
using ApiPoo2.Domain.Common;

namespace ApiPoo2.Application.CQRS.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, OperationResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken)
            ?? throw new NotFoundException("user.not_found", "El usuario no existe.");

        if (!_passwordHasher.Verify(command.CurrentPassword, user.PasswordHash.Hash))
        {
            throw new UnauthorizedException("password.incorrect", "La contraseña actual es incorrecta.");
        }

        PasswordPolicy.EnsureValid(command.NewPassword);

        var newHash = _passwordHasher.Hash(command.NewPassword);
        user.ChangePassword(newHash, _dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }
}