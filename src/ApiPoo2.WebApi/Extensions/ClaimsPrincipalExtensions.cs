using System.Security.Claims;

namespace ApiPoo2.WebApi.Extensions;

public static class ClaimsPrincipalExtensions
{
    private const string SubClaim = "sub";
    private const string JtiClaim = "jti";
    private const string ExpClaim = "exp";
    private const string NameClaim = "name";

    public static Guid GetPersonaId(this ClaimsPrincipal principal)
        => Guid.Parse(principal.FindFirstValue(SubClaim)
            ?? throw new UnauthorizedAccessException("El token no contiene el identificador del usuario."));

    /// <summary>Login textual del usuario autenticado (claim `name` del JWT).</summary>
    public static string GetLogin(this ClaimsPrincipal principal)
        => principal.FindFirstValue(NameClaim)
            ?? principal.FindFirstValue(ClaimTypes.Name)
            ?? throw new UnauthorizedAccessException("El token no contiene el login del usuario.");

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
