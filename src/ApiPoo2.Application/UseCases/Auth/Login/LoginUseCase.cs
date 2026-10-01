using ApiPoo2.Application.UseCases.Auth;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Users;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Auth;

public sealed class LoginUseCase : BaseUseCase<LoginInputDto, LoginTokenPairOutputDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public LoginUseCase(
        IEnumerable<IValidator<LoginInputDto>> validators,
        ILoggerFactory loggerFactory,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<LoginTokenPairOutputDto> ExecuteCoreAsync(LoginInputDto request, CancellationToken cancellationToken)
    {
        var login = Login.From(request.Login);
        var now = _dateTimeProvider.UtcNow;

        var user = await _userRepository.GetByLoginAsync(login, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException("credentials.invalid", "Credenciales inválidas.");
        }

        switch (user.EvaluateAuthentication(now))
        {
            case AuthenticationBlock.Inactive:
                throw new UnauthorizedException("account.inactive", "La cuenta está desactivada.");
            case AuthenticationBlock.LockedOut:
                throw new UnauthorizedException("account.locked", "Cuenta bloqueada temporalmente por demasiados intentos fallidos.");
        }

        var passwordValid = _passwordHasher.Verify(request.Password, user.PasswordHash.Hash);

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

        return LoginTokenPairOutputDto.Create(access, refresh, now, user.ApiKey);
    }
}
