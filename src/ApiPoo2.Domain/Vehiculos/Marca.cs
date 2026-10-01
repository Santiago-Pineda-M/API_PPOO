using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Vehiculos;

public sealed class Marca
{
    public const int MaxLength = 50;

    public string Value { get; }

    private Marca(string value) => Value = value;

    public static Marca From(string value)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add("La marca es obligatoria.");
        }
        else if (value.Trim().Length > MaxLength)
        {
            errors.Add($"La marca no puede superar los {MaxLength} caracteres.");
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException("vehiculo.marca", errors);
        }

        return new Marca(value.Trim());
    }

    public bool Equals(Marca? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is Marca otro && Equals(otro);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(Marca? left, Marca? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Marca? left, Marca? right) => !(left == right);

    public override string ToString() => Value;
}