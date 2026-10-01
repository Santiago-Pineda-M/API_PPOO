using ApiPoo2.Application.IServices;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Users;

namespace ApiPoo2.Application.UseCases.Auth;

/// <summary>
///     La APIKey viaja en el login porque es la única vía para que el cliente la obtenga sin
///     tener que regenerarla; los servicios protegidos la exigen en la cabecera.
/// </summary>
public sealed record LoginTokenPairOutputDto(
    string AccessToken,
    string AccessTokenType,
    DateTime AccessTokenExpiresAtUtc,
    int AccessTokenExpiresInSeconds,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    string ApiKey)
{
    public static LoginTokenPairOutputDto Create(
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
