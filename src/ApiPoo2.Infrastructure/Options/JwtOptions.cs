namespace ApiPoo2.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int AccessTokenTtlMinutes { get; set; } = 15;

    public int RefreshTokenTtlDays { get; set; } = 7;
}