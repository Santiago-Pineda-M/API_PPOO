using ApiPoo2.Domain.RefreshTokens;
namespace ApiPoo2.Application.UseCases.Auth;

public sealed record RevokeRefreshTokenInputDto(Guid PersonaId, string RefreshToken);
