namespace ApiPoo2.Domain.Enums;

public enum RevocationReason
{
    Logout = 0,
    Rotation = 1,
    SecurityBreach = 2,
    SessionLimitReached = 3,
    ExplicitRevocation = 4,
}