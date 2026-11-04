using System.IdentityModel.Tokens.Jwt;
using ApiPoo2.Application.IServices;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Users;
using ApiPoo2.Infrastructure.Security;
using FluentAssertions;

namespace ApiPoo2.InfrastructureTests.Security;

public sealed class JwtTokenServiceTests
{
    private static readonly DateTime Ahora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static JwtTokenService CrearServicio(int accessTtlMinutos = 15, int refreshTtlDias = 7)
        => new(new JwtOptions
        {
            Secret = "secreto-de-prueba-con-mas-de-32-caracteres-123456",
            Issuer = "ApiPoo2",
            Audience = "ApiPoo2",
            AccessTokenTtlMinutes = accessTtlMinutos,
            RefreshTokenTtlDays = refreshTtlDias,
        });

    private static User CrearUsuario(string identificacion = "1234567890")
    {
        var persona = Persona.Register(
            TipoIdentificacion.CedulaCiudadania,
            identificacion,
            "Juan",
            "Pérez",
            $"juan-{identificacion}@example.com",
            TipoPersona.Administrativo,
            Ahora);

        return persona.CrearUsuario("HASH:Password123!", Ahora);
    }

    private static JwtSecurityToken LeerToken(string tokenCrudo)
        => new JwtSecurityTokenHandler().ReadJwtToken(tokenCrudo);

    [Fact]
    public void JwtTokenService_CreateAccessToken_EmiteClaimsDeIdentidad()
    {
        var servicio = CrearServicio();
        var usuario = CrearUsuario();

        var descriptor = servicio.CreateAccessToken(usuario, Ahora);
        var token = LeerToken(descriptor.Token);

        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub
            && c.Value == usuario.IdPersona.ToString());
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Name
            && c.Value == usuario.Login.Value);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti
            && c.Value == descriptor.Jti.ToString());
    }

    [Fact]
    public void JwtTokenService_CreateAccessToken_AplicaEmisorAudienciaYExpiracion()
    {
        var servicio = CrearServicio(accessTtlMinutos: 30);
        var usuario = CrearUsuario();

        var descriptor = servicio.CreateAccessToken(usuario, Ahora);
        var token = LeerToken(descriptor.Token);

        token.Issuer.Should().Be("ApiPoo2");
        token.Audiences.Should().Contain("ApiPoo2");
        token.ValidTo.ToUniversalTime()
            .Should().BeCloseTo(Ahora.AddMinutes(30), TimeSpan.FromSeconds(1));
        descriptor.ExpiresAtUtc.Should().Be(Ahora.AddMinutes(30));
    }

    [Fact]
    public void JwtTokenService_CreateAccessToken_GeneraJtiUnico()
    {
        var servicio = CrearServicio();
        var usuario = CrearUsuario();

        var primero = servicio.CreateAccessToken(usuario, Ahora);
        var segundo = servicio.CreateAccessToken(usuario, Ahora);

        primero.Jti.Should().NotBe(segundo.Jti);
        primero.Token.Should().NotBe(segundo.Token);
    }

    [Fact]
    public void JwtTokenService_CreateRefreshToken_GeneraValorUnicoConExpiracion()
    {
        var servicio = CrearServicio(refreshTtlDias: 7);

        var primero = servicio.CreateRefreshToken(Ahora);
        var segundo = servicio.CreateRefreshToken(Ahora);

        primero.RawValue.Should().NotBeNullOrWhiteSpace();
        primero.RawValue.Should().NotBe(segundo.RawValue);
        primero.ExpiresAtUtc.Should().Be(Ahora.AddDays(7));
    }

    [Fact]
    public void JwtTokenService_CreateRefreshToken_HashEsDeterminista()
    {
        var servicio = CrearServicio();
        var descriptor = servicio.CreateRefreshToken(Ahora);

        servicio.HashRefreshToken(descriptor.RawValue).Should().Be(descriptor.Hash);
    }

    [Fact]
    public void JwtTokenService_HashRefreshToken_EsEstableYSensibleAlValor()
    {
        var servicio = CrearServicio();

        var hash = servicio.HashRefreshToken("valor-crudo");

        servicio.HashRefreshToken("valor-crudo").Should().Be(hash);
        servicio.HashRefreshToken("otro-valor").Should().NotBe(hash);
        hash.Should().NotContain("valor-crudo");
    }
}
