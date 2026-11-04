using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Rutas;

/// <summary>
///     Código que agrupa las paradas de una ruta. Es la clave de negocio: se normaliza a mayúsculas
///     y sin espacios para que dos cargues con distinta capitalización no creen rutas duplicadas.
/// </summary>
public sealed class CodigoRuta
{
    public const int MaxLength = 32;

    public string Value { get; }

    private CodigoRuta(string value) => Value = value;

    public static CodigoRuta From(string value)
    {
        var normalizado = value?.Trim().ToUpperInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizado))
        {
            throw new DomainValidationException(
                "ruta.codigo.requerido",
                ["El código de ruta es obligatorio."]);
        }

        if (normalizado.Length > MaxLength)
        {
            throw new DomainValidationException(
                "ruta.codigo.longitud",
                [$"El código de ruta no puede superar los {MaxLength} caracteres."]);
        }

        return new CodigoRuta(normalizado);
    }

    public bool Equals(CodigoRuta? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is CodigoRuta otro && Equals(otro);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(CodigoRuta? left, CodigoRuta? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(CodigoRuta? left, CodigoRuta? right) => !(left == right);

    public override string ToString() => Value;
}
