using ApiPoo2.Application.IServices;

namespace ApiPoo2.Application.DTOs;

public sealed record RefreshTokenPairDto(
    string AccessToken,
    string AccessTokenType,
    DateTime AccessTokenExpiresAtUtc,
    int AccessTokenExpiresInSeconds,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc)
{
    public static RefreshTokenPairDto Create(AccessTokenDescriptor access, RefreshTokenDescriptor refresh, DateTime utcNow) => new(
        access.Token,
        access.TokenType,
        access.ExpiresAtUtc,
        Math.Max(0, (int)(access.ExpiresAtUtc - utcNow).TotalSeconds),
        refresh.RawValue,
        refresh.ExpiresAtUtc);
}