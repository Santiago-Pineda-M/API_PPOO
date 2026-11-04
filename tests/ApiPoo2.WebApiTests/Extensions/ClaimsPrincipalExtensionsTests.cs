using System.Security.Claims;
using ApiPoo2.WebApi.Extensions;
using FluentAssertions;

namespace ApiPoo2.WebApiTests.Extensions;

public sealed class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal ConstruirPrincipal(params (string Tipo, string Valor)[] claims)
    {
        var identity = new ClaimsIdentity(claims.Select(c => new Claim(c.Tipo, c.Valor)), "Test");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public void GetPersonaId_LeeElClaimSub()
    {
        var principal = ConstruirPrincipal(("sub", "11111111-2222-3333-4444-555555555555"));

        principal.GetPersonaId().Should().Be(Guid.Parse("11111111-2222-3333-4444-555555555555"));
    }

    [Fact]
    public void GetPersonaId_SinClaimSub_LanzaExcepcion()
    {
        var principal = ConstruirPrincipal(("jti", Guid.NewGuid().ToString()));

        var act = () => principal.GetPersonaId();

        var exception = act.Should().Throw<UnauthorizedAccessException>();
        exception.Which.Message.Should().Contain("identificador del usuario");
    }

    [Fact]
    public void GetPersonaId_ClaimSubInvalido_LanzaFormatException()
    {
        var principal = ConstruirPrincipal(("sub", "no-es-un-guid"));

        var act = () => principal.GetPersonaId();

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void GetLogin_LeeElClaimName()
    {
        var principal = ConstruirPrincipal(("name", "rm1"));

        principal.GetLogin().Should().Be("rm1");
    }

    [Fact]
    public void GetLogin_SinClaimName_UsaElClaimNameDeIdentity()
    {
        var principal = ConstruirPrincipal((ClaimTypes.Name, "rm2"));

        principal.GetLogin().Should().Be("rm2");
    }

    [Fact]
    public void GetLogin_SinClaimsDeNombre_LanzaExcepcion()
    {
        var principal = ConstruirPrincipal(("sub", Guid.NewGuid().ToString()));

        var act = () => principal.GetLogin();

        var exception = act.Should().Throw<UnauthorizedAccessException>();
        exception.Which.Message.Should().Contain("login");
    }

    [Fact]
    public void GetTokenJti_LeeElClaimJti()
    {
        var jti = Guid.NewGuid();
        var principal = ConstruirPrincipal(("jti", jti.ToString()));

        principal.GetTokenJti().Should().Be(jti);
    }

    [Fact]
    public void GetTokenJti_SinClaimJti_LanzaExcepcion()
    {
        var principal = ConstruirPrincipal(("sub", Guid.NewGuid().ToString()));

        var act = () => principal.GetTokenJti();

        var exception = act.Should().Throw<UnauthorizedAccessException>();
        exception.Which.Message.Should().Contain("jti");
    }

    [Fact]
    public void GetTokenExpiresAtUtc_ConvierteElClaimExp()
    {
        var fecha = new DateTimeOffset(2026, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var principal = ConstruirPrincipal(("exp", fecha.ToUnixTimeSeconds().ToString()));

        principal.GetTokenExpiresAtUtc().Should().Be(fecha.UtcDateTime);
    }

    [Fact]
    public void GetTokenExpiresAtUtc_SinClaimExp_LanzaExcepcion()
    {
        var principal = ConstruirPrincipal(("sub", Guid.NewGuid().ToString()));

        var act = () => principal.GetTokenExpiresAtUtc();

        var exception = act.Should().Throw<UnauthorizedAccessException>();
        exception.Which.Message.Should().Contain("expiración");
    }

    [Fact]
    public void GetTokenExpiresAtUtc_ClaimExpInvalido_LanzaExcepcion()
    {
        var principal = ConstruirPrincipal(("exp", "no-es-un-numero"));

        var act = () => principal.GetTokenExpiresAtUtc();

        act.Should().Throw<FormatException>();
    }
}
