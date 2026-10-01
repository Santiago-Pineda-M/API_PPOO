using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.RefreshTokens;

namespace ApiPoo2.Domain.Users;

/// <summary>
///     Usuario del sistema. El enunciado exige primary key compuesta por (idpersona, login) y una
///     persona tiene uno y solo un usuario, por eso esta entidad <b>no</b> hereda de
///     <see cref="BaseEntity" />: la identidad ya no es un Guid único sino el par persona + login.
/// </summary>
public sealed class User
{
    public const int MaxFailedAccessAttempts = 5;

    public const int MaxActiveRefreshTokens = 10;

    public static readonly TimeSpan DefaultLockoutDuration = TimeSpan.FromMinutes(15);

    public Guid IdPersona { get; private set; }

    public Login Login { get; private set; } = null!;

    public PasswordHash PasswordHash { get; private set; } = null!;

    /// <summary>
    ///     Algoritmo del hash. Es una propiedad propia (y no una proyección) porque EF necesita
    ///     una columna materializable para mapearla.
    /// </summary>
    public string PasswordHashAlgorithm { get; private set; } = PasswordHash.DefaultAlgorithm;

    public ApiKey ApiKey { get; private set; } = null!;

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; } = true;

    public int AccessFailedCount { get; private set; }

    public DateTimeOffset? LockoutEndUtc { get; private set; }

    public DateTime? LastLoginAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    private List<RefreshToken> RefreshTokens { get; set; } = [];

    public IReadOnlyList<RefreshToken> RefreshTokensVisibles => RefreshTokens;

    private User()
    {
    }

    public static User Crear(
        Guid idPersona,
        Login login,
        string passwordHash,
        UserRole role,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(login);

        if (idPersona == Guid.Empty)
        {
            throw new DomainValidationException(
                "user.persona",
                ["La persona del usuario es obligatoria."]);
        }

        return new User
        {
            IdPersona = idPersona,
            Login = login,
            PasswordHash = PasswordHash.Create(passwordHash),
            PasswordHashAlgorithm = PasswordHash.Create(passwordHash).Algorithm,
            ApiKey = Domain.Personas.ApiKey.Generate(),
            Role = role,
            IsActive = true,
            CreatedAtUtc = utcNow,
        };
    }

    public int CountActiveSessions(DateTime utcNow) => RefreshTokens.Count(t => t.IsActive(utcNow));

    public RefreshToken? FindRefreshToken(string tokenHash)
        => RefreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);

    public bool IsLockedOut(DateTime utcNow) => LockoutEndUtc is { } end && end > utcNow;

    public void Deactivate(DateTime utcNow)
    {
        IsActive = false;
        RevokeAllRefreshTokens(RevocationReason.AccountDeactivated, utcNow);
        MarkUpdated(utcNow);
    }

    public void Activate(DateTime utcNow)
    {
        IsActive = true;
        AccessFailedCount = 0;
        LockoutEndUtc = null;
        MarkUpdated(utcNow);
    }

    public AuthenticationBlock EvaluateAuthentication(DateTime utcNow)
    {
        if (!IsActive)
        {
            return AuthenticationBlock.Inactive;
        }

        return IsLockedOut(utcNow) ? AuthenticationBlock.LockedOut : AuthenticationBlock.None;
    }

    public void ChangePassword(string newPasswordHash, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new DomainValidationException("password.invalid", ["La nueva contraseña es obligatoria."]);
        }

        PasswordHash = PasswordHash.Create(newPasswordHash);
        RevokeAllRefreshTokens(RevocationReason.PasswordChanged, utcNow);
        MarkUpdated(utcNow);
    }

    /// <summary>
    ///     Regenera la APIKey. El enunciado pide un servicio GET para rehacerla.
    /// </summary>
    public void RegenerarApiKey(DateTime utcNow)
    {
        ApiKey = Domain.Personas.ApiKey.Generate();
        MarkUpdated(utcNow);
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
            }
        }

        MarkUpdated(utcNow);
    }

    public RefreshToken IssueRefreshToken(string tokenHash, DateTime expiresAtUtc, DateTime utcNow)
    {
        EnforceActiveSessionLimit(utcNow);

        var token = RefreshToken.Issue(IdPersona, Login, tokenHash, expiresAtUtc, utcNow);
        RefreshTokens.Add(token);
        return token;
    }

    public void RevokeAllRefreshTokens(RevocationReason reason, DateTime utcNow)
    {
        var revoked = false;

        foreach (var token in RefreshTokens.Where(t => !t.IsRevoked).ToList())
        {
            token.Revoke(reason, utcNow);
            revoked = true;
        }

        if (revoked)
        {
            MarkUpdated(utcNow);
        }
    }

    private void EnforceActiveSessionLimit(DateTime utcNow)
    {
        var actives = RefreshTokens
            .Where(t => t.IsActive(utcNow))
            .OrderBy(t => t.CreatedAtUtc)
            .ToList();

        if (actives.Count < MaxActiveRefreshTokens)
        {
            return;
        }

        actives[0].Revoke(RevocationReason.SessionLimitReached, utcNow);
    }

    private void MarkUpdated(DateTime utcNow) => UpdatedAtUtc = utcNow;
}
