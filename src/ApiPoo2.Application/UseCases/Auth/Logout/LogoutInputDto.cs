using ApiPoo2.Domain.RefreshTokens;
namespace ApiPoo2.Application.UseCases.Auth;

public sealed record LogoutInputDto(
    Guid PersonaId,
    Guid AccessTokenJti,
    DateTime AccessTokenExpiresAtUtc,
    string? RefreshToken);
