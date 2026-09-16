using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Exceptions;
using FluentAssertions;

namespace ApiPoo2.UnitTests.Domain;

public sealed class PasswordPolicyTests
{
    [Theory]
    [InlineData("Password123!")]
    [InlineData("Tr0bador#99")]
    [InlineData("aB3!cD4$eF5")]
    public void EnsureValid_Should_AcceptStrongPassword(string password)
    {
        var act = () => PasswordPolicy.EnsureValid(password);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("short")]                            // muy corta
    [InlineData("password")]                         // sin mayúsculas ni número ni símbolo
    [InlineData("PASSWORD123")]                      // sin minúsculas ni símbolo
    [InlineData("Password123")]                      // sin símbolo
    [InlineData("Pass!!word")]        // sin número
    public void EnsureValid_Should_RejectWeakPassword(string password)
    {
        var act = () => PasswordPolicy.EnsureValid(password);
        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("password.policy");
    }

    [Fact]
    public void EnsureValid_Should_ReportAllMissingRules()
    {
        var act = () => PasswordPolicy.EnsureValid("abc");
        var ex = act.Should().Throw<DomainValidationException>().Which;

        ex.Code.Should().Be("password.policy");
        ex.Errors.Should().Contain(e => e.Contains("mayúscula"));
        ex.Errors.Should().Contain(e => e.Contains("número"));
    }
}