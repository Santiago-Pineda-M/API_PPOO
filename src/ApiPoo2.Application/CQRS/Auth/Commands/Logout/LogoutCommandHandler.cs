using ApiPoo2.Application.Exceptions;
using ApiPoo2.Application.Models;
using ApiPoo2.Application.CQRS;
using ApiPoo2.Application.IRepositories;
using ApiPoo2.Application.IServices;
using ApiPoo2.Domain.Enums;

namespace ApiPoo2.Application.CQRS.Auth.Commands.Logout;

public sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand, OperationResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IJwtTokenBlacklistService _blacklistService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutCommandHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IJwtTokenBlacklistService blacklistService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _blacklistService = blacklistService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        await _blacklistService.BlacklistAsync(command.AccessTokenJti, command.UserId, command.AccessTokenExpiresAtUtc, cancellationToken);

        if (!string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            var user = await _userRepository.GetByIdWithRefreshTokensAsync(command.UserId, cancellationToken);

            if (user is not null)
            {
                var hash = _jwtTokenService.HashRefreshToken(command.RefreshToken);
                var token = user.RefreshTokens.FirstOrDefault(t => t.TokenHash == hash);

                if (token is not null)
                {
                    user.RevokeRefreshToken(token.Id, RevocationReason.Logout, now);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
        }

        return OperationResult.Success();
    }
}