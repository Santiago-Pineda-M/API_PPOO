using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Documentos;

public sealed class DocumentoCodigo
{
    public const int MaxLength = 30;

    public string Value { get; }

    private DocumentoCodigo(string value) => Value = value;

    public static DocumentoCodigo From(string value)
    {
        var errors = new List<string>();
        var normalizado = value?.Trim().ToUpperInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizado))
        {
            errors.Add("El código del documento es obligatorio.");
        }
        else if (normalizado.Length > MaxLength)
        {
            errors.Add($"El código del documento no puede superar los {MaxLength} caracteres.");
        }
        else if (!normalizado.All(c => char.IsLetterOrDigit(c) || c == '_'))
        {
            errors.Add("El código del documento solo puede contener letras, dígitos y guion bajo.");
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException("documento.codigo", errors);
        }

        return new DocumentoCodigo(normalizado);
    }

    public bool Equals(DocumentoCodigo? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is DocumentoCodigo otro && Equals(otro);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(DocumentoCodigo? left, DocumentoCodigo? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(DocumentoCodigo? left, DocumentoCodigo? right) => !(left == right);

    public override string ToString() => Value;
}
