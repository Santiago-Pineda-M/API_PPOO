using ApiPoo2.Domain.Common;
using FluentAssertions;

namespace ApiPoo2.DomainTests.Domain;

public sealed class PasswordPolicyTests
{
    private const string MensajeObligatoria = "La contraseña es obligatoria.";

    [Theory]
    [InlineData("Password1!")]
    [InlineData("Aa1!aaaa")]
    [InlineData("Segur0@2026")]
    [InlineData("Password123!")]
    [InlineData("Tr0bador#99")]
    [InlineData("aB3!cD4$eF5")]
    public void PasswordPolicy_EnsureValid_AceptaPasswordConCategoria(string password)
        => FluentActions.Invoking(() => PasswordPolicy.EnsureValid(password)).Should().NotThrow();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void PasswordPolicy_EnsureValid_RechazaPasswordVacio(string? password)
    {
        var exception = FluentActions.Invoking(() => PasswordPolicy.EnsureValid(password!))
            .Should().Throw<DomainValidationException>().Which;

        exception.Code.Should().Be("password.policy");
        exception.Errors.Should().Contain(MensajeObligatoria);
    }

    [Fact]
    public void PasswordPolicy_EnsureValid_RechazaPasswordCorta()
    {
        var exception = FluentActions.Invoking(() => PasswordPolicy.EnsureValid("Aa1!aa"))
            .Should().Throw<DomainValidationException>().Which;

        exception.Errors.Should().Contain($"La contraseña debe tener al menos {PasswordPolicy.MinimumLength} caracteres.");
    }

    [Fact]
    public void PasswordPolicy_EnsureValid_RechazaPasswordLarga()
    {
        var password = new string('a', PasswordPolicy.MaximumLength) + "A1!";

        var exception = FluentActions.Invoking(() => PasswordPolicy.EnsureValid(password))
            .Should().Throw<DomainValidationException>().Which;

        exception.Errors.Should().Contain($"La contraseña no puede superar los {PasswordPolicy.MaximumLength} caracteres.");
    }

    [Theory]
    [InlineData("aa1!aaaa", "mayúscula")]
    [InlineData("AA1!AAAA", "minúscula")]
    [InlineData("Aa!!aaaa", "número")]
    [InlineData("Aa1aaaaa", "especial")]
    [InlineData("password", "mayúscula")]
    [InlineData("PASSWORD123", "minúscula")]
    [InlineData("Password123", "especial")]
    [InlineData("Pass!!word", "número")]
    public void PasswordPolicy_EnsureValid_RechazaPasswordSinCategoria(string password, string categoriaFaltante)
    {
        var exception = FluentActions.Invoking(() => PasswordPolicy.EnsureValid(password))
            .Should().Throw<DomainValidationException>().Which;

        exception.Errors.Should().Contain(e => e.Contains(categoriaFaltante));
    }

    [Fact]
    public void PasswordPolicy_EnsureValid_ReportaTodasLasReglasFaltantes()
    {
        var exception = FluentActions.Invoking(() => PasswordPolicy.EnsureValid("abc"))
            .Should().Throw<DomainValidationException>().Which;

        exception.Code.Should().Be("password.policy");
        exception.Errors.Should().HaveCount(4);
        exception.Errors.Should().Contain(e => e.Contains("mayúscula"));
        exception.Errors.Should().Contain(e => e.Contains("número"));
    }

    [Fact]
    public void PasswordPolicy_EnsureValid_RechazaPasswordSoloSinSimbolo()
    {
        var exception = FluentActions.Invoking(() => PasswordPolicy.EnsureValid("Password123"))
            .Should().Throw<DomainValidationException>().Which;

        exception.Errors.Should().HaveCount(1);
        exception.Errors[0].Should().Contain("especial");
    }
}
