using ApiPoo2.Domain.Entities;
using ApiPoo2.Domain.Enums;
using ApiPoo2.Domain.Events;
using FluentAssertions;

namespace ApiPoo2.UnitTests.Domain;

public sealed class UserTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Register_Should_CreateUserAndRaiseEvent()
    {
        var user = User.Register("John@Example.com", "hash", UserRole.Member, Now);

        user.Id.Should().NotBeEmpty();
        user.Email.Value.Should().Be("john@example.com");
        user.Role.Should().Be(UserRole.Member);
        user.IsActive.Should().BeTrue();
        user.CreatedAtUtc.Should().Be(Now);
        user.DomainEvents.Should().ContainSingle(e => e is UserRegisteredEvent);
    }

    [Fact]
    public void ChangePassword_Should_UpdateHashAndRaiseEvent()
    {
        var user = RegisterFixture();
        user.IsLockedOut(Now).Should().BeFalse();

        user.ChangePassword("newHash", Now);

        user.PasswordHash.Hash.Should().Be("newHash");
        user.UpdatedAtUtc.Should().Be(Now);
        user.DomainEvents.Should().ContainSingle(e => e is PasswordChangedEvent);
    }

    [Fact]
    public void RecordLoginAttempt_Should_ResetCounterOnSuccess()
    {
        var user = RegisterFixture();

        user.RecordLoginAttempt(false, Now);
        user.RecordLoginAttempt(false, Now);
        user.AccessFailedCount.Should().Be(2);

        user.RecordLoginAttempt(true, Now);

        user.AccessFailedCount.Should().Be(0);
        user.LockoutEndUtc.Should().BeNull();
        user.LastLoginAtUtc.Should().Be(Now);
    }

    [Fact]
    public void RecordLoginAttempt_Should_LockAccountAfterMaxFailures()
    {
        var user = RegisterFixture();

        for (var i = 0; i < User.MaxFailedAccessAttempts; i++)
        {
            user.RecordLoginAttempt(false, Now);
        }

        user.IsLockedOut(Now).Should().BeTrue();
        user.LockoutEndUtc.Should().Be(Now.Add(User.DefaultLockoutDuration));
        user.DomainEvents.Should().ContainSingle(e => e is UserLockedOutEvent);
    }

    [Fact]
    public void Lockout_Should_ExpireAfterDuration()
    {
        var user = RegisterFixture();

        for (var i = 0; i < User.MaxFailedAccessAttempts; i++)
        {
            user.RecordLoginAttempt(false, Now);
        }

        user.IsLockedOut(Now).Should().BeTrue();
        user.IsLockedOut(Now.Add(User.DefaultLockoutDuration).AddSeconds(1)).Should().BeFalse();
    }

    [Fact]
    public void RecordLoginAttempt_Should_BeNoOpWhileLocked()
    {
        var user = RegisterFixture();
        user.RecordLoginAttempt(false, Now);
        user.RecordLoginAttempt(false, Now);
        user.RecordLoginAttempt(false, Now);
        user.RecordLoginAttempt(false, Now);
        user.RecordLoginAttempt(false, Now);
        var lockedAt = user.LockoutEndUtc;

        user.RecordLoginAttempt(true, Now);

        user.IsLockedOut(Now).Should().BeTrue();
        user.LockoutEndUtc.Should().Be(lockedAt);
    }

    [Fact]
    public void IssueRefreshToken_Should_AddTokenToCollection()
    {
        var user = RegisterFixture();

        var token = user.IssueRefreshToken("hash", Now.AddDays(7), Now);

        token.UserId.Should().Be(user.Id);
        user.RefreshTokens.Should().ContainSingle(t => t.Id == token.Id);
    }

    [Fact]
    public void IssueRefreshToken_Should_RevokeOldestActiveWhenLimitReached()
    {
        var user = RegisterFixture();
        var tokens = new List<RefreshToken>();

        for (var i = 0; i < User.MaxActiveRefreshTokens + 1; i++)
        {
            tokens.Add(user.IssueRefreshToken($"hash-{i}", Now.AddDays(7), Now.AddMinutes(i + 1)));
        }

        user.RefreshTokens.Should().HaveCount(User.MaxActiveRefreshTokens + 1);
        tokens[0].IsRevoked.Should().BeTrue();
        tokens[0].RevokedReason.Should().Be(RevocationReason.SessionLimitReached);
        user.RefreshTokens.Count(t => t.IsActive(Now)).Should().Be(User.MaxActiveRefreshTokens);
    }

    [Fact]
    public void RevokeAllRefreshTokens_Should_RevokeAllAndRaiseEvents()
    {
        var user = RegisterFixture();
        user.IssueRefreshToken("hash-1", Now.AddDays(7), Now);
        user.IssueRefreshToken("hash-2", Now.AddDays(7), Now);

        user.RevokeAllRefreshTokens(RevocationReason.SecurityBreach, Now);

        user.RefreshTokens.Should().OnlyContain(t => t.IsRevoked);
        user.DomainEvents.OfType<RefreshTokenRevokedEvent>()
            .Should().HaveCount(2).And.OnlyContain(e => e.Reason == RevocationReason.SecurityBreach);
    }

    private static User RegisterFixture() =>
        User.Register("john@example.com", "hash", UserRole.Member, Now);
}