using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Personas;

public sealed class NumeroIdentificacion
{
    public const int MaxLength = 20;

    public string Value { get; }

    private NumeroIdentificacion(string value) => Value = value;

    public static NumeroIdentificacion From(string value)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add("El número de identificación es obligatorio.");
        }
        else if (value.Length > MaxLength)
        {
            errors.Add($"El número de identificación no puede superar los {MaxLength} caracteres.");
        }
        else if (!value.All(char.IsDigit))
        {
            errors.Add("El número de identificación solo puede contener dígitos.");
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException("persona.identificacion", errors);
        }

        return new NumeroIdentificacion(value);
    }

    public bool Equals(NumeroIdentificacion? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is NumeroIdentificacion otro && Equals(otro);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(NumeroIdentificacion? left, NumeroIdentificacion? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(NumeroIdentificacion? left, NumeroIdentificacion? right) => !(left == right);

    public override string ToString() => Value;
}
