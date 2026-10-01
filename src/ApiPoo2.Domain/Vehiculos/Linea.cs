using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Vehiculos;

public sealed class Linea
{
    public const int MaxLength = 50;

    public string Value { get; }

    private Linea(string value) => Value = value;

    public static Linea From(string value)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add("La línea es obligatoria.");
        }
        else if (value.Trim().Length > MaxLength)
        {
            errors.Add($"La línea no puede superar los {MaxLength} caracteres.");
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException("vehiculo.linea", errors);
        }

        return new Linea(value.Trim());
    }

    public bool Equals(Linea? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is Linea otro && Equals(otro);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(Linea? left, Linea? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Linea? left, Linea? right) => !(left == right);

    public override string ToString() => Value;
}