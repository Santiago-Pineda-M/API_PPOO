using ApiPoo2.Application.IRepositories;
using ApiPoo2.Application.IServices;
using ApiPoo2.Application.DTOs;
using ApiPoo2.Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Auth.Logout;

public sealed class LogoutUseCase : BaseUseCase<LogoutInputDto, OperationResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IJwtTokenBlacklistService _blacklistService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutUseCase(
        IEnumerable<IValidator<LogoutInputDto>> validators,
        ILoggerFactory loggerFactory,
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IJwtTokenBlacklistService blacklistService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _blacklistService = blacklistService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<OperationResult> ExecuteCoreAsync(LogoutInputDto request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        await _blacklistService.BlacklistAsync(request.AccessTokenJti, request.UserId, request.AccessTokenExpiresAtUtc, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var user = await _userRepository.GetByIdWithRefreshTokensAsync(request.UserId, cancellationToken);

            if (user is not null)
            {
                var hash = _jwtTokenService.HashRefreshToken(request.RefreshToken);
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