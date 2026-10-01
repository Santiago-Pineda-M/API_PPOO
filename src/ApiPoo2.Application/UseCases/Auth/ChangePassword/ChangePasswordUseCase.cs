using ApiPoo2.Domain.Users;
using ApiPoo2.Application.UseCases.Auth;
using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.RefreshTokens;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Auth;

public sealed class ChangePasswordUseCase : BaseUseCase<ChangePasswordInputDto, OperationResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenBlacklistService _blacklistService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ChangePasswordUseCase(
        IEnumerable<IValidator<ChangePasswordInputDto>> validators,
        ILoggerFactory loggerFactory,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenBlacklistService blacklistService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _blacklistService = blacklistService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<OperationResult> ExecuteCoreAsync(ChangePasswordInputDto request, CancellationToken cancellationToken)
    {
        var persona = await _userRepository.GetPersonaByIdAsync(request.PersonaId, cancellationToken)
            ?? throw new NotFoundException("person.not_found", "La persona no existe.");

        var user = persona.Usuario
            ?? throw new NotFoundException("user.not_found", "La persona no tiene un usuario asociado.");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash.Hash))
        {
            throw new UnauthorizedException("password.incorrect", "La contraseña actual es incorrecta.");
        }

        PasswordPolicy.EnsureValid(request.NewPassword);

        var now = _dateTimeProvider.UtcNow;
        var newHash = _passwordHasher.Hash(request.NewPassword);

        user.ChangePassword(newHash, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _blacklistService.BlacklistAsync(request.AccessTokenJti, persona.Id, request.AccessTokenExpiresAtUtc, cancellationToken);

        return OperationResult.Success();
    }
}
