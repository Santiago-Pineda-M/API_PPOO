using ApiPoo2.Application.Exceptions;
using ApiPoo2.Application.Models;
using ApiPoo2.Application.CQRS;
using ApiPoo2.Application.IRepositories;
using ApiPoo2.Application.IServices;
using ApiPoo2.Domain.Entities;

namespace ApiPoo2.Application.CQRS.Auth.Commands.Login;

public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, TokenPairDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<TokenPairDto> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email.Trim().ToLowerInvariant();
        var now = _dateTimeProvider.UtcNow;

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException("credentials.invalid", "Credenciales inválidas.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("account.inactive", "La cuenta está desactivada.");
        }

        if (user.IsLockedOut(now))
        {
            throw new UnauthorizedException("account.locked", "Cuenta bloqueada temporalmente por demasiados intentos fallidos.");
        }

        var passwordValid = _passwordHasher.Verify(command.Password, user.PasswordHash.Hash);

        user.RecordLoginAttempt(passwordValid, now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!passwordValid)
        {
            throw new UnauthorizedException("credentials.invalid", "Credenciales inválidas.");
        }

        var access = _jwtTokenService.CreateAccessToken(user, now);
        var refresh = _jwtTokenService.CreateRefreshToken(now);

        var refreshToken = user.IssueRefreshToken(refresh.Hash, refresh.ExpiresAtUtc, now);
        _refreshTokenRepository.Add(refreshToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TokenPairDto.Create(access, refresh, now);
    }
}