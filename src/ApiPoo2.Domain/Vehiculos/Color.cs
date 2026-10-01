using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Vehiculos;

/// <summary>Color del vehículo en representación hexadecimal. Ejemplo: #FF5733.</summary>
public sealed class Color
{
    public const int MaxLength = 7;

    public string Value { get; }

    private Color(string value) => Value = value;

    public static Color From(string value)
    {
        var errors = new List<string>();
        var normalizado = value?.Trim().ToUpperInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizado))
        {
            errors.Add("El color es obligatorio.");
        }
        else if (normalizado.Length != MaxLength)
        {
            errors.Add($"El color debe tener {MaxLength} caracteres en hexadecimal. Ejemplo: #FF5733.");
        }
        else if (normalizado[0] != '#' || !normalizado[1..].All(IsHexDigit))
        {
            errors.Add("El color debe tener el formato #RRGGBB. Ejemplo: #FF5733.");
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException("vehiculo.color", errors);
        }

        return new Color(normalizado);
    }

    private static bool IsHexDigit(char c)
        => char.IsDigit(c) || c is >= 'A' and <= 'F' || c is >= 'a' and <= 'f';

    public bool Equals(Color? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is Color otro && Equals(otro);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(Color? left, Color? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Color? left, Color? right) => !(left == right);

    public override string ToString() => Value;
}