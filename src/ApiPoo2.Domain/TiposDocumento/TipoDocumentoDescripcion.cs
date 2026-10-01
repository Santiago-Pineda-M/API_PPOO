using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.TiposDocumento;

public sealed class TipoDocumentoDescripcion
{
    public const int MaxLength = 500;

    public string Value { get; }

    private TipoDocumentoDescripcion(string value) => Value = value;

    public static TipoDocumentoDescripcion From(string value)
    {
        var errors = new List<string>();
        var normalizado = value?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizado))
        {
            errors.Add("La descripción del documento es obligatoria.");
        }
        else if (normalizado.Length > MaxLength)
        {
            errors.Add($"La descripción no puede superar los {MaxLength} caracteres.");
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException("documento.descripcion", errors);
        }

        return new TipoDocumentoDescripcion(normalizado);
    }

    public bool Equals(TipoDocumentoDescripcion? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is TipoDocumentoDescripcion otro && Equals(otro);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(TipoDocumentoDescripcion? left, TipoDocumentoDescripcion? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(TipoDocumentoDescripcion? left, TipoDocumentoDescripcion? right)
        => !(left == right);

    public override string ToString() => Value;
}