using ApiPoo2.Domain.RefreshTokens;
using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Personas;
using FluentAssertions;

namespace ApiPoo2.UnitTests.Domain;

public sealed class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Issue_Should_CreateUpcomingToken()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), Login.From("jp123"), "hash", Now.AddDays(1), Now);

        token.IsActive(Now).Should().BeTrue();
        token.IsExpired(Now.AddDays(2)).Should().BeTrue();
        token.IsRevoked.Should().BeFalse();
        token.IsUsed.Should().BeFalse();
    }

    [Fact]
    public void Issue_Should_RejectExpiryInThePast()
    {
        var act = () => RefreshToken.Issue(Guid.NewGuid(), Login.From("jp123"), "hash", Now.AddMinutes(-1), Now);
        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("refresh.invalid");
    }

    [Fact]
    public void Issue_Should_RejectLifetimeBeyondMaximum()
    {
        var act = () => RefreshToken.Issue(Guid.NewGuid(), Login.From("jp123"), "hash", Now.AddDays(8), Now);
        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("refresh.invalid");
    }

    [Fact]
    public void Issue_Should_RejectEmptyHash()
    {
        var act = () => RefreshToken.Issue(Guid.NewGuid(), Login.From("jp123"), "  ", Now.AddDays(1), Now);
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void RotateTo_Should_ConsumeTheOpportunityTokenOnce()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), Login.From("jp123"), "hash-a", Now.AddDays(1), Now);
        var replacement = RefreshToken.Issue(Guid.NewGuid(), Login.From("jp123"), "hash-b", Now.AddDays(1), Now);

        token.RotateTo(replacement);

        token.IsUsed.Should().BeTrue();
        token.IsRevoked.Should().BeTrue();
        token.UsedAtUtc.Should().Be(Now);
        token.RevokedReason.Should().Be(RevocationReason.Rotation);
        token.ReplacedByTokenId.Should().Be(replacement.Id);
        token.IsActive(Now).Should().BeFalse();
    }

    [Fact]
    public void RotateTo_Should_ThrowOnSecondRotation()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), Login.From("jp123"), "hash-a", Now.AddDays(1), Now);
        token.RotateTo(RefreshToken.Issue(Guid.NewGuid(), Login.From("jp123"), "hash-b", Now.AddDays(1), Now));

        var act = () => token.RotateTo(RefreshToken.Issue(Guid.NewGuid(), Login.From("jp123"), "hash-c", Now.AddDays(1), Now));

        act.Should().Throw<DomainException>().Which.Code.Should().Be("refresh.already_used");
    }

    [Fact]
    public void Revoke_Should_BeIdempotent()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), Login.From("jp123"), "hash", Now.AddDays(1), Now);

        token.Revoke(RevocationReason.Logout, Now);
        token.Revoke(RevocationReason.SecurityBreach, Now);

        token.IsRevoked.Should().BeTrue();
        token.RevokedReason.Should().Be(RevocationReason.Logout);
    }
}
