using ApiPoo2.Application.Exceptions;
using ApiPoo2.Application.IRepositories;
using ApiPoo2.Application.IServices;
using ApiPoo2.Application.DTOs;
using ApiPoo2.Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Auth.RevokeRefreshToken;

public sealed class RevokeRefreshTokenUseCase : BaseUseCase<RevokeRefreshTokenInputDto, OperationResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeRefreshTokenUseCase(
        IEnumerable<IValidator<RevokeRefreshTokenInputDto>> validators,
        ILoggerFactory loggerFactory,
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<OperationResult> ExecuteCoreAsync(RevokeRefreshTokenInputDto request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithRefreshTokensAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("user.not_found", "El usuario no existe.");

        var hash = _jwtTokenService.HashRefreshToken(request.RefreshToken);
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