using System.Security.Claims;

namespace ApiPoo2.WebApi.Extensions;

public static class ClaimsPrincipalExtensions
{
    private const string SubClaim = "sub";
    private const string JtiClaim = "jti";
    private const string ExpClaim = "exp";

    public static Guid GetUserId(this ClaimsPrincipal principal)
        => Guid.Parse(principal.FindFirstValue(SubClaim)
            ?? throw new UnauthorizedAccessException("El token no contiene el identificador del usuario."));

    public static Guid GetTokenJti(this ClaimsPrincipal principal)
        => Guid.Parse(principal.FindFirstValue(JtiClaim)
            ?? throw new UnauthorizedAccessException("El token no contiene identificador (jti)."));

    public static DateTime GetTokenExpiresAtUtc(this ClaimsPrincipal principal)
    {
        var exp = long.Parse(principal.FindFirstValue(ExpClaim)
            ?? throw new UnauthorizedAccessException("El token no contiene fecha de expiración."));

        return DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
    }
}