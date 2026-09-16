using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Enums;
using ApiPoo2.Domain.Events;
using ApiPoo2.Domain.Exceptions;
using ApiPoo2.Domain.ValueObjects;

namespace ApiPoo2.Domain.Entities;

public class User : BaseEntity, IAggregateRoot
{
    public const int MaxFailedAccessAttempts = 5;
    public const int MaxActiveRefreshTokens = 10;
    public static readonly TimeSpan DefaultLockoutDuration = TimeSpan.FromMinutes(15);

    private readonly List<RefreshToken> _refreshTokens = [];

    public Email Email { get; private set; } = null!;

    public PasswordHash PasswordHash { get; private set; } = null!;

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; } = true;

    public int AccessFailedCount { get; private set; }

    public DateTimeOffset? LockoutEndUtc { get; private set; }

    public DateTime? LastLoginAtUtc { get; private set; }

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User()
    {
    }

    public static User Register(
        string email,
        string passwordHash,
        UserRole role,
        DateTime utcNow)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = utcNow,
            Email = Email.From(email),
            PasswordHash = PasswordHash.Create(passwordHash),
            Role = role,
            IsActive = true,
        };

        user.RaiseDomainEvent(new UserRegisteredEvent(user.Id, user.Email.Value, utcNow));
        return user;
    }

    public void ChangePassword(string newPasswordHash, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new DomainValidationException("password.invalid", ["La nueva contraseña es obligatoria."]);
        }

        PasswordHash = PasswordHash.Create(newPasswordHash);
        MarkUpdated(utcNow);
        RaiseDomainEvent(new PasswordChangedEvent(Id, utcNow));
    }

    public void RecordLoginAttempt(bool success, DateTime utcNow)
    {
        if (IsLockedOut(utcNow))
        {
            return;
        }

        if (success)
        {
            AccessFailedCount = 0;
            LockoutEndUtc = null;
            LastLoginAtUtc = utcNow;
        }
        else
        {
            AccessFailedCount++;

            if (AccessFailedCount >= MaxFailedAccessAttempts)
            {
                LockoutEndUtc = DateTime.SpecifyKind(utcNow.Add(DefaultLockoutDuration), DateTimeKind.Utc);
                AccessFailedCount = 0;
                RaiseDomainEvent(new UserLockedOutEvent(Id, LockoutEndUtc.Value, utcNow));
            }
        }

        MarkUpdated(utcNow);
    }

    public bool IsLockedOut(DateTime utcNow) => LockoutEndUtc is { } end && end > utcNow;

    public RefreshToken IssueRefreshToken(string tokenHash, DateTime expiresAtUtc, DateTime utcNow)
    {
        EnforceActiveSessionLimit(utcNow);

        var token = RefreshToken.Issue(Id, tokenHash, expiresAtUtc, utcNow);
        _refreshTokens.Add(token);
        return token;
    }

    public void RevokeRefreshToken(Guid refreshTokenId, RevocationReason reason, DateTime utcNow)
    {
        var token = _refreshTokens.FirstOrDefault(t => t.Id == refreshTokenId)
            ?? throw new DomainException("refresh.not_found", "No se encontró el token de refresco.");

        if (token.IsRevoked)
        {
            return;
        }

        token.Revoke(reason, utcNow);
        RaiseDomainEvent(new RefreshTokenRevokedEvent(Id, token.Id, reason, utcNow));
        MarkUpdated(utcNow);
    }

    public void RevokeAllRefreshTokens(RevocationReason reason, DateTime utcNow)
    {
        var revoked = false;

        foreach (var token in _refreshTokens.Where(t => !t.IsRevoked).ToList())
        {
            token.Revoke(reason, utcNow);
            RaiseDomainEvent(new RefreshTokenRevokedEvent(Id, token.Id, reason, utcNow));
            revoked = true;
        }

        if (revoked)
        {
            MarkUpdated(utcNow);
        }
    }

    private void EnforceActiveSessionLimit(DateTime utcNow)
    {
        var actives = _refreshTokens
            .Where(t => t.IsActive(utcNow))
            .OrderBy(t => t.CreatedAtUtc)
            .ToList();

        if (actives.Count < MaxActiveRefreshTokens)
        {
            return;
        }

        var oldest = actives[0];
        oldest.Revoke(RevocationReason.SessionLimitReached, utcNow);
        RaiseDomainEvent(new RefreshTokenRevokedEvent(Id, oldest.Id, RevocationReason.SessionLimitReached, utcNow));
    }
}