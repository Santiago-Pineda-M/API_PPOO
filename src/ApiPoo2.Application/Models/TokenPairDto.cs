using ApiPoo2.Application.IServices;

namespace ApiPoo2.Application.Models;

public sealed record TokenPairDto(
    string AccessToken,
    string AccessTokenType,
    DateTime AccessTokenExpiresAtUtc,
    int AccessTokenExpiresInSeconds,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc)
{
    public static TokenPairDto Create(AccessTokenDescriptor access, RefreshTokenDescriptor refresh, DateTime utcNow) => new(
        access.Token,
        access.TokenType,
        access.ExpiresAtUtc,
        Math.Max(0, (int)(access.ExpiresAtUtc - utcNow).TotalSeconds),
        refresh.RawValue,
        refresh.ExpiresAtUtc);
}