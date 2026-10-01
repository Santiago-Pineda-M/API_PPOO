using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Personas;

namespace ApiPoo2.Domain.RefreshTokens;

public sealed class RefreshToken : BaseEntity
{
    public static readonly TimeSpan MaxLifetime = TimeSpan.FromDays(7);

    public Guid UserId { get; private set; }

    /// <summary>
    ///     Login del dueño. Junto con <see cref="UserId" /> forma la clave foránea hacia
    ///     Usuario, cuya primary key es compuesta (idpersona, login).
    /// </summary>
    public Login UserLogin { get; private set; } = null!;

    public string TokenHash { get; private set; } = null!;

    public DateTime ExpiresAtUtc { get; private set; }

    public bool IsUsed { get; private set; }

    public DateTime? UsedAtUtc { get; private set; }

    public bool IsRevoked { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public RevocationReason? RevokedReason { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    private RefreshToken()
    {
    }

    public static RefreshToken Issue(
        Guid userId,
        Login userLogin,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainValidationException("refresh.invalid", ["El usuario del token de refresco es obligatorio."]);
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainValidationException("refresh.invalid", ["El hash del token de refresco es obligatorio."]);
        }

        if (expiresAtUtc <= utcNow)
        {
            throw new DomainValidationException("refresh.invalid", ["La validez del token de refresco debe ser futura."]);
        }

        if (expiresAtUtc - utcNow > MaxLifetime)
        {
            throw new DomainValidationException("refresh.invalid", ["El token de refresco supera la vida máxima permitida."]);
        }

        var token = new RefreshToken
        {
            UserId = userId,
            UserLogin = userLogin,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
        };

        token.Initialize(Guid.NewGuid(), utcNow);
        return token;
    }

    public bool IsActive(DateTime utcNow) => !IsUsed && !IsRevoked && utcNow < ExpiresAtUtc;

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;

    public bool WasRotated() => IsUsed;

    public void RotateTo(RefreshToken replacement)
    {
        if (replacement is null || replacement.Id == Id)
        {
            throw new DomainException("refresh.invalid", "El token de reemplazo no es válido.");
        }

        if (IsUsed)
        {
            throw new DomainException("refresh.already_used", "El token de refresco ya fue utilizado.");
        }

        IsUsed = true;
        UsedAtUtc = replacement.CreatedAtUtc;
        IsRevoked = true;
        RevokedAtUtc = replacement.CreatedAtUtc;
        RevokedReason = RevocationReason.Rotation;
        ReplacedByTokenId = replacement.Id;
    }

    public void Revoke(RevocationReason reason, DateTime utcNow)
    {
        if (IsRevoked)
        {
            return;
        }

        IsRevoked = true;
        RevokedAtUtc = utcNow;
        RevokedReason = reason;
        MarkUpdated(utcNow);
    }
}
