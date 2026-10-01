using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Personas;

public sealed class Nombres
{
    public const int MaxLength = 100;

    public string Value { get; }

    private Nombres(string value) => Value = value;

    public static Nombres From(string value)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add("Los nombres son obligatorios.");
        }
        else if (value.Trim().Length > MaxLength)
        {
            errors.Add($"Los nombres no pueden superar los {MaxLength} caracteres.");
        }
        else if (!value.Any(char.IsLetter))
        {
            errors.Add("Los nombres deben contener al menos una letra.");
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException("persona.nombres", errors);
        }

        return new Nombres(value.Trim());
    }

    public bool Equals(Nombres? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is Nombres otro && Equals(otro);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(Nombres? left, Nombres? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Nombres? left, Nombres? right) => !(left == right);

    public override string ToString() => Value;
}
