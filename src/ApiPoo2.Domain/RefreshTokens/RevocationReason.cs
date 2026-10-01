namespace ApiPoo2.Domain.RefreshTokens;

public enum RevocationReason
{
    Logout = 0,
    Rotation = 1,
    SecurityBreach = 2,
    SessionLimitReached = 3,
    ExplicitRevocation = 4,
    PasswordChanged = 5,
    AccountDeactivated = 6,
}
