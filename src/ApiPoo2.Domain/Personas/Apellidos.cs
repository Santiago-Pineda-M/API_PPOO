using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Personas;

public sealed class Apellidos
{
    public const int MaxLength = 100;

    public string Value { get; }

    private Apellidos(string value) => Value = value;

    public static Apellidos From(string value)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add("Los apellidos son obligatorios.");
        }
        else if (value.Trim().Length > MaxLength)
        {
            errors.Add($"Los apellidos no pueden superar los {MaxLength} caracteres.");
        }
        else if (!value.Any(char.IsLetter))
        {
            errors.Add("Los apellidos deben contener al menos una letra.");
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException("persona.apellidos", errors);
        }

        return new Apellidos(value.Trim());
    }

    public bool Equals(Apellidos? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is Apellidos otro && Equals(otro);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(Apellidos? left, Apellidos? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Apellidos? left, Apellidos? right) => !(left == right);

    public override string ToString() => Value;
}
