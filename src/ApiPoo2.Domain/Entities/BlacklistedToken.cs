using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Exceptions;

namespace ApiPoo2.Domain.Entities;

public class BlacklistedToken : BaseEntity
{
    public Guid Jti { get; private set; }

    public Guid UserId { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime RevokedAtUtc { get; private set; }

    private BlacklistedToken()
    {
    }

    public static BlacklistedToken Create(Guid jti, Guid userId, DateTime expiresAtUtc, DateTime utcNow)
    {
        if (jti == Guid.Empty)
        {
            throw new DomainValidationException("access.blacklist_invalid", ["El identificador del token es obligatorio."]);
        }

        if (expiresAtUtc <= utcNow)
        {
            throw new DomainValidationException("access.blacklist_invalid", ["El token ya expiró, no requiere revocación."]);
        }

        return new BlacklistedToken
        {
            Id = Guid.NewGuid(),
            Jti = jti,
            UserId = userId,
            ExpiresAtUtc = expiresAtUtc,
            RevokedAtUtc = utcNow,
            CreatedAtUtc = utcNow,
        };
    }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;
}