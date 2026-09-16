using ApiPoo2.Application.Exceptions;
using ApiPoo2.Application.Models;
using ApiPoo2.Application.CQRS;
using ApiPoo2.Application.IRepositories;
using ApiPoo2.Application.IServices;
using ApiPoo2.Domain.Enums;

namespace ApiPoo2.Application.CQRS.Auth.Commands.RevokeRefreshToken;

public sealed class RevokeRefreshTokenCommandHandler : ICommandHandler<RevokeRefreshTokenCommand, OperationResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeRefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult> Handle(RevokeRefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithRefreshTokensAsync(command.UserId, cancellationToken)
            ?? throw new UnauthorizedException("user.not_found", "El usuario no existe.");

        var hash = _jwtTokenService.HashRefreshToken(command.RefreshToken);
        var token = user.RefreshTokens.FirstOrDefault(t => t.TokenHash == hash);

        if (token is null || token.IsRevoked)
        {
            return OperationResult.Success();
        }

        user.RevokeRefreshToken(token.Id, RevocationReason.ExplicitRevocation, _dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }
}