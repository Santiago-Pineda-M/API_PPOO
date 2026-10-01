using ApiPoo2.Application.IServices;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.RefreshTokens;
namespace ApiPoo2.Application.UseCases.Auth;

public sealed record RefreshTokenPairOutputDto(
    string AccessToken,
    string AccessTokenType,
    DateTime AccessTokenExpiresAtUtc,
    int AccessTokenExpiresInSeconds,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    string ApiKey)
{
    public static RefreshTokenPairOutputDto Create(
        AccessTokenDescriptor access,
        RefreshTokenDescriptor refresh,
        DateTime utcNow,
        ApiKey apiKey) => new(
        access.Token,
        access.TokenType,
        access.ExpiresAtUtc,
        Math.Max(0, (int)(access.ExpiresAtUtc - utcNow).TotalSeconds),
        refresh.RawValue,
        refresh.ExpiresAtUtc,
        apiKey.Value);
}
