using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.BlacklistedTokens;

public sealed class BlacklistedToken : BaseEntity
{
    public Guid Jti { get; private set; }

    /// <summary>Persona titular del token revocado. Antes era el id de usuario.</summary>
    public Guid PersonaId { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime RevokedAtUtc { get; private set; }

    private BlacklistedToken()
    {
    }

    public static BlacklistedToken Create(Guid jti, Guid personaId, DateTime expiresAtUtc, DateTime utcNow)
    {
        if (jti == Guid.Empty)
        {
            throw new DomainValidationException("access.blacklist_invalid", ["El identificador del token es obligatorio."]);
        }

        if (expiresAtUtc <= utcNow)
        {
            throw new DomainValidationException("access.blacklist_invalid", ["El token ya expiró, no requiere revocación."]);
        }

        var blacklisted = new BlacklistedToken
        {
            Jti = jti,
            PersonaId = personaId,
            ExpiresAtUtc = expiresAtUtc,
            RevokedAtUtc = utcNow,
        };

        blacklisted.Initialize(Guid.NewGuid(), utcNow);
        return blacklisted;
    }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;
}
