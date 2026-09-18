namespace ApiPoo2.Application.DTOs;

public sealed record RevokeRefreshTokenInputDto(Guid UserId, string RefreshToken);