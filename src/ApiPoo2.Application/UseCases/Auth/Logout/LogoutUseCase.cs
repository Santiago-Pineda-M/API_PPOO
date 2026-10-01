using ApiPoo2.Domain.RefreshTokens;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Auth;

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

    protected override async Task<OperationResult> ExecuteCoreAsync(
        LogoutInputDto request,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        await _blacklistService.BlacklistAsync(
            request.AccessTokenJti,
            request.PersonaId,
            request.AccessTokenExpiresAtUtc,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return OperationResult.Success();
        }

        var persona = await _userRepository.GetPersonaByIdAsync(request.PersonaId, cancellationToken)
            ?? throw new NotFoundException("person.not_found", "La persona no existe.");

        var user = persona.Usuario
            ?? throw new NotFoundException("user.not_found", "La persona no tiene un usuario asociado.");

        var hash = _jwtTokenService.HashRefreshToken(request.RefreshToken);
        user.FindRefreshToken(hash)?.Revoke(RevocationReason.Logout, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }
}
