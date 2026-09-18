namespace ApiPoo2.Application.DTOs;

public sealed record LogoutInputDto(
    Guid UserId,
    Guid AccessTokenJti,
    DateTime AccessTokenExpiresAtUtc,
    string? RefreshToken);