using ApiPoo2.Application.Exceptions;
using ApiPoo2.Application.IRepositories;
using ApiPoo2.Application.IServices;
using ApiPoo2.Application.DTOs;
using ApiPoo2.Domain.Common;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Auth.ChangePassword;

public sealed class ChangePasswordUseCase : BaseUseCase<ChangePasswordInputDto, OperationResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ChangePasswordUseCase(
        IEnumerable<IValidator<ChangePasswordInputDto>> validators,
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

    protected override async Task<OperationResult> ExecuteCoreAsync(ChangePasswordInputDto request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("user.not_found", "El usuario no existe.");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash.Hash))
        {
            throw new UnauthorizedException("password.incorrect", "La contraseña actual es incorrecta.");
        }

        PasswordPolicy.EnsureValid(request.NewPassword);

        var newHash = _passwordHasher.Hash(request.NewPassword);
        user.ChangePassword(newHash, _dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }
}