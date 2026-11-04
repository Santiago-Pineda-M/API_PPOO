using ApiPoo2.Infrastructure.Security;
using FluentAssertions;

namespace ApiPoo2.InfrastructureTests.Security;

public sealed class PasswordHasherTests
{
    [Fact]
    public void PasswordHasher_Hash_GeneraHashDistintoCadaVez()
    {
        var hasher = new PasswordHasher();

        var primero = hasher.Hash("Password123!");
        var segundo = hasher.Hash("Password123!");

        primero.Should().NotBe(segundo);
    }

    [Fact]
    public void PasswordHasher_Hash_NoContieneElTextoPlano()
    {
        var hasher = new PasswordHasher();

        hasher.Hash("Password123!").Should().NotContain("Password123!");
    }

    [Fact]
    public void PasswordHasher_Verify_AceptaPasswordCorrecta()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("Password123!");

        hasher.Verify("Password123!", hash).Should().BeTrue();
    }

    [Fact]
    public void PasswordHasher_Verify_RechazaPasswordIncorrecta()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("Password123!");

        hasher.Verify("OtraPassword456?", hash).Should().BeFalse();
    }

    [Fact]
    public void PasswordHasher_Verify_RechazaHashAjena()
    {
        var hasher = new PasswordHasher();

        var act = () => hasher.Verify("Password123!", "no-es-un-hash-bcrypt");

        act.Should().Throw<BCrypt.Net.SaltParseException>();
    }
}
