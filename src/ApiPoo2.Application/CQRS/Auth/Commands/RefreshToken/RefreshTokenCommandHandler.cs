using ApiPoo2.Application.Exceptions;
using ApiPoo2.Application.Models;
using ApiPoo2.Application.CQRS;
using ApiPoo2.Application.IRepositories;
using ApiPoo2.Application.IServices;
using ApiPoo2.Domain.Enums;

namespace ApiPoo2.Application.CQRS.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, TokenPairDto>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<TokenPairDto> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var hash = _jwtTokenService.HashRefreshToken(command.RefreshToken);
        var storedToken = await _refreshTokenRepository.GetByHashAsync(hash, cancellationToken);

        if (storedToken is null)
        {
            throw new UnauthorizedException("refresh.invalid", "Token de refresco inválido.");
        }

        if (storedToken.IsUsed)
        {
            var compromised = await _userRepository.GetByIdWithRefreshTokensAsync(storedToken.UserId, cancellationToken);
            compromised?.RevokeAllRefreshTokens(RevocationReason.SecurityBreach, now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("refresh.reuse", "Se detectó reuso de token de refresco. Sesión revocada.");
        }

        if (!storedToken.IsActive(now))
        {
            throw new UnauthorizedException("refresh.invalid", "Token de refresco expirado o revocado.");
        }

        var user = await _userRepository.GetByIdWithRefreshTokensAsync(storedToken.UserId, cancellationToken)
            ?? throw new UnauthorizedException("refresh.invalid", "Token de refresco inválido.");

        var access = _jwtTokenService.CreateAccessToken(user, now);
        var refresh = _jwtTokenService.CreateRefreshToken(now);

        var replacement = user.IssueRefreshToken(refresh.Hash, refresh.ExpiresAtUtc, now);
        _refreshTokenRepository.Add(replacement);
        storedToken.RotateTo(replacement);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TokenPairDto.Create(access, refresh, now);
    }
}