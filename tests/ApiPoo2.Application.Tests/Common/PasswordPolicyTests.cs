using ApiPoo2.Domain.Common;
using FluentAssertions;

namespace ApiPoo2.Application.Tests.Common;

/// <summary>
///     La política de contraseña vive fuera de la entidad porque <c>User</c> recibe el hash, no el
///     secreto. Estos tests fijan su comportamiento para que ningún caso de uso pueda saltársela.
/// </summary>
public sealed class PasswordPolicyTests
{
    [Theory]
    [InlineData("Password1!")]
    [InlineData("Aa1!aaaa")]
    [InlineData("Segur0@2026")]
    public void EnsureValid_AcceptsCompliantPassword(string password)
        => FluentActions.Invoking(() => PasswordPolicy.EnsureValid(password)).Should().NotThrow();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EnsureValid_RejectsMissingPassword(string? password)
    {
        var exception = FluentActions.Invoking(() => PasswordPolicy.EnsureValid(password!))
            .Should().Throw<DomainValidationException>().Which;

        exception.Code.Should().Be("password.policy");
        exception.Errors.Should().Contain("La contraseña es obligatoria.");
    }

    [Fact]
    public void EnsureValid_RejectsTooShortPassword()
    {
        var exception = FluentActions.Invoking(() => PasswordPolicy.EnsureValid("Aa1!aa"))
            .Should().Throw<DomainValidationException>().Which;

        exception.Errors.Should().Contain($"La contraseña debe tener al menos {PasswordPolicy.MinimumLength} caracteres.");
    }

    [Fact]
    public void EnsureValid_RejectsTooLongPassword()
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
    public void EnsureValid_RejectsMissingComposition(string password, string missingRequirement)
    {
        var exception = FluentActions.Invoking(() => PasswordPolicy.EnsureValid(password))
            .Should().Throw<DomainValidationException>().Which;

        exception.Errors.Should().HaveCount(1);
        exception.Errors[0].Should().Contain(missingRequirement);
    }

    [Fact]
    public void EnsureValid_ReportsEveryViolationAtOnce()
    {
        var exception = FluentActions.Invoking(() => PasswordPolicy.EnsureValid("abc"))
            .Should().Throw<DomainValidationException>().Which;

        exception.Code.Should().Be("password.policy");
        exception.Errors.Should().HaveCount(4);
    }
}
