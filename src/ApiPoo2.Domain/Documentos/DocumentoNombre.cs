using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Vehiculos;

namespace ApiPoo2.Domain.Documentos;

public sealed class DocumentoNombre
{
    public const int MaxLength = 100;

    public string Value { get; }

    private DocumentoNombre(string value) => Value = value;

    public static DocumentoNombre From(string value)
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

        return new DocumentoNombre(value.Trim());
    }

    public bool Equals(DocumentoNombre? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is DocumentoNombre otro && Equals(otro);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(DocumentoNombre? left, DocumentoNombre? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(DocumentoNombre? left, DocumentoNombre? right) => !(left == right);

    public override string ToString() => Value;
}
