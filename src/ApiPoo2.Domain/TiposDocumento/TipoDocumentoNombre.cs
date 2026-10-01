using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Vehiculos;

namespace ApiPoo2.Domain.TiposDocumento;

public sealed class TipoDocumentoNombre
{
    public const int MaxLength = 100;

    public string Value { get; }

    private TipoDocumentoNombre(string value) => Value = value;

    public static TipoDocumentoNombre From(string value)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add("El nombre del documento es obligatorio.");
        }
        else if (value.Trim().Length > MaxLength)
        {
            errors.Add($"El nombre del documento no puede superar los {MaxLength} caracteres.");
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException("documento.nombre", errors);
        }

        return new TipoDocumentoNombre(value.Trim());
    }

    public bool Equals(TipoDocumentoNombre? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is TipoDocumentoNombre otro && Equals(otro);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(TipoDocumentoNombre? left, TipoDocumentoNombre? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(TipoDocumentoNombre? left, TipoDocumentoNombre? right) => !(left == right);

    public override string ToString() => Value;
}
