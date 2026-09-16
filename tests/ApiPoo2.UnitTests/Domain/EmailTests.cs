using ApiPoo2.Domain.Exceptions;
using ApiPoo2.Domain.ValueObjects;
using FluentAssertions;

namespace ApiPoo2.UnitTests.Domain;

public sealed class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData(" first.last+tag@ExampleBrazil.co ")]
    public void From_Should_NormalizeValidEmail(string input)
    {
        var email = Email.From(input);
        email.Value.Should().Be(input.Trim().ToLowerInvariant());
    }

    [Fact]
    public void From_Should_ThrowOnNullEmail()
    {
        var act = () => Email.From(null!);
        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("email.invalid");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("user@")]
    [InlineData("@example.com")]
    [InlineData("user@example")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa@example.com")]
    public void From_Should_ThrowOnInvalidEmail(string input)
    {
        var act = () => Email.From(input);
        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("email.invalid");
    }

    [Fact]
    public void From_Should_ProduceValueEquality()
    {
        var a = Email.From("User@Example.com");
        var b = Email.From("user@example.com");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}