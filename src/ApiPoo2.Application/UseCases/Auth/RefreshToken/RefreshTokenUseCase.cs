using ApiPoo2.Application.UseCases.Auth;
using ApiPoo2.Domain.RefreshTokens;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Auth;

public sealed class RefreshTokenUseCase : BaseUseCase<RefreshTokenInputDto, RefreshTokenPairOutputDto>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenUseCase(
        IEnumerable<IValidator<RefreshTokenInputDto>> validators,
        ILoggerFactory loggerFactory,
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<RefreshTokenPairOutputDto> ExecuteCoreAsync(RefreshTokenInputDto request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var hash = _jwtTokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await _refreshTokenRepository.GetByHashAsync(hash, cancellationToken);

        if (storedToken is null)
        {
            throw new UnauthorizedException("refresh.invalid", "Token de refresco inválido.");
        }

        var persona = await _userRepository.GetPersonaByIdAsync(storedToken.UserId, cancellationToken)
            ?? throw new UnauthorizedException("refresh.invalid", "Token de refresco inválido.");

        var user = persona.Usuario
            ?? throw new UnauthorizedException("refresh.invalid", "Token de refresco inválido.");

        if (storedToken.WasRotated())
        {
            user.RevokeAllRefreshTokens(RevocationReason.SecurityBreach, now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("refresh.reuse", "Se detectó reuso de token de refresco. Sesión revocada.");
        }

        if (!storedToken.IsActive(now))
        {
            throw new UnauthorizedException("refresh.invalid", "Token de refresco expirado o revocado.");
        }

        var access = _jwtTokenService.CreateAccessToken(user, now);
        var refresh = _jwtTokenService.CreateRefreshToken(now);

        var replacement = user.IssueRefreshToken(refresh.Hash, refresh.ExpiresAtUtc, now);
        _refreshTokenRepository.Add(replacement);
        storedToken.RotateTo(replacement);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return RefreshTokenPairOutputDto.Create(access, refresh, now, user.ApiKey);
    }
}
