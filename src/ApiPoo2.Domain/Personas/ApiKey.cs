using System.Security.Cryptography;
using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Personas;

public sealed class ApiKey
{
    public const int Length = 48;

    public string Value { get; }

    private ApiKey(string value) => Value = value;

    public static ApiKey From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException(
                "user.apikey",
                ["La APIKey es obligatoria."]);
        }

        return new ApiKey(value);
    }

    public static ApiKey Generate() => new(Convert.ToHexString(RandomNumberGenerator.GetBytes(Length)));

    public bool Equals(ApiKey? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is ApiKey otro && Equals(otro);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(ApiKey? left, ApiKey? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(ApiKey? left, ApiKey? right) => !(left == right);

    public override string ToString() => Value;
}
