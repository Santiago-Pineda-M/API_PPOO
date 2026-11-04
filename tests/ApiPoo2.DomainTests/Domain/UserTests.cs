using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.RefreshTokens;
using ApiPoo2.Domain.Users;
using FluentAssertions;

namespace ApiPoo2.UnitTests.Domain;

public sealed class UserTests
{
    private static readonly DateTime Ahora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static User CrearUsuario(string passwordHash = "hash-valido", UserRole role = UserRole.Administrator)
        => User.Crear(
            Guid.NewGuid(),
            Login.From("jp123"),
            passwordHash,
            role,
            Ahora);

    [Fact]
    public void Crear_AsignaIdPersonaYLoginYGeneraApiKey()
    {
        var personaId = Guid.NewGuid();

        var user = User.Crear(personaId, Login.From("jp123"), "hash", UserRole.Administrator, Ahora);

        user.IdPersona.Should().Be(personaId);
        user.Login.Value.Should().Be("jp123");
        user.ApiKey.Value.Should().NotBeNullOrWhiteSpace();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Crear_RechazaPersonaVacia()
    {
        FluentActions.Invoking(() => User.Crear(Guid.Empty, Login.From("jp123"), "hash", UserRole.Administrator, Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("user.persona");
    }

    [Fact]
    public void Crear_RechazaHashVacio()
        => FluentActions.Invoking(() => User.Crear(Guid.NewGuid(), Login.From("jp123"), "  ", UserRole.Administrator, Ahora))
            .Should().Throw<DomainValidationException>();

    [Fact]
    public void Crear_IniciaSinRefreshTokens()
        => CrearUsuario().CountActiveSessions(Ahora).Should().Be(0);

    [Fact]
    public void IssueRefreshToken_RegistraElToken()
    {
        var user = CrearUsuario();

        user.IssueRefreshToken("hash-1", Ahora.AddDays(7), Ahora);

        user.CountActiveSessions(Ahora).Should().Be(1);
    }

    [Fact]
    public void IssueRefreshToken_LimitaASesionesActivas()
    {
        var user = CrearUsuario();

        for (var i = 0; i < User.MaxActiveRefreshTokens; i++)
        {
            // Todas dentro de la vida máxima: lo que se agota es el límite de sesiones, no el TTL.
            user.IssueRefreshToken($"hash-{i}", Ahora.AddDays(1).AddMinutes(i), Ahora);
        }

        user.IssueRefreshToken("hash-extra", Ahora.AddDays(1).AddMinutes(50), Ahora);

        user.CountActiveSessions(Ahora).Should().Be(User.MaxActiveRefreshTokens);
    }

    [Fact]
    public void FindRefreshToken_EncuentraPorHash()
    {
        var user = CrearUsuario();
        user.IssueRefreshToken("hash-abc", Ahora.AddDays(7), Ahora);

        user.FindRefreshToken("hash-abc").Should().NotBeNull();
        user.FindRefreshToken("otro").Should().BeNull();
    }

    [Fact]
    public void RevokeAllRefreshTokens_RevocaTodos()
    {
        var user = CrearUsuario();
        user.IssueRefreshToken("hash-1", Ahora.AddDays(7), Ahora);
        user.IssueRefreshToken("hash-2", Ahora.AddDays(7), Ahora);

        user.RevokeAllRefreshTokens(RevocationReason.SecurityBreach, Ahora);

        user.CountActiveSessions(Ahora).Should().Be(0);
    }

    [Fact]
    public void EvaluateAuthentication_InactivoCuandoEstaDesactivado()
    {
        var user = CrearUsuario();
        user.Deactivate(Ahora);

        user.EvaluateAuthentication(Ahora).Should().Be(AuthenticationBlock.Inactive);
    }

    [Fact]
    public void EvaluateAuthentication_BloqueadoTrasCincoFallos()
    {
        var user = CrearUsuario();

        for (var i = 0; i < User.MaxFailedAccessAttempts; i++)
        {
            user.RecordLoginAttempt(false, Ahora);
        }

        user.EvaluateAuthentication(Ahora).Should().Be(AuthenticationBlock.LockedOut);
    }

    [Fact]
    public void RecordLoginAttempt_ExitoReseteaElContador()
    {
        var user = CrearUsuario();
        user.RecordLoginAttempt(false, Ahora);

        user.RecordLoginAttempt(true, Ahora);

        user.AccessFailedCount.Should().Be(0);
        user.LastLoginAtUtc.Should().Be(Ahora);
    }

    [Fact]
    public void ChangePassword_RevocaLasSesiones()
    {
        var user = CrearUsuario();
        user.IssueRefreshToken("hash-1", Ahora.AddDays(7), Ahora);

        user.ChangePassword("nuevo-hash", Ahora);

        user.CountActiveSessions(Ahora).Should().Be(0);
        user.PasswordHash.Hash.Should().Be("nuevo-hash");
    }

    [Fact]
    public void ChangePassword_RechazaHashVacio()
        => FluentActions.Invoking(() => CrearUsuario().ChangePassword("  ", Ahora))
            .Should().Throw<DomainValidationException>();

    [Fact]
    public void RegenerarApiKey_CambiaElValor()
    {
        var user = CrearUsuario();
        var anterior = user.ApiKey.Value;

        user.RegenerarApiKey(Ahora);

        user.ApiKey.Value.Should().NotBe(anterior);
    }

    [Fact]
    public void Activar_RestauraLaCuenta()
    {
        var user = CrearUsuario();
        user.Deactivate(Ahora);

        user.Activate(Ahora);

        user.EvaluateAuthentication(Ahora).Should().Be(AuthenticationBlock.None);
    }
}
