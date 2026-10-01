using ApiPoo2.Application.UseCases.Auth;
using ApiPoo2.Domain.RefreshTokens;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Auth;

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
        var persona = await _userRepository.GetPersonaByIdAsync(request.PersonaId, cancellationToken)
            ?? throw new NotFoundException("person.not_found", "La persona no existe.");

        var user = persona.Usuario
            ?? throw new NotFoundException("user.not_found", "La persona no tiene un usuario asociado.");

        var hash = _jwtTokenService.HashRefreshToken(request.RefreshToken);
        user.FindRefreshToken(hash)?.Revoke(RevocationReason.ExplicitRevocation, _dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }
}
