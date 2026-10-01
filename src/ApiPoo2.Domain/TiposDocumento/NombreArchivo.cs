using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.TiposDocumento;

public sealed class NombreArchivo
{
    public const int MaxLength = 255;

    public string Value { get; }

    private NombreArchivo(string value) => Value = value;

    public static NombreArchivo From(string value)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add("El nombre del archivo es obligatorio.");
        }
        else if (value.Trim().Length > MaxLength)
        {
            errors.Add($"El nombre del archivo no puede superar los {MaxLength} caracteres.");
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException("documento.archivo", errors);
        }

        return new NombreArchivo(value.Trim());
    }

    public bool Equals(NombreArchivo? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is NombreArchivo otro && Equals(otro);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(NombreArchivo? left, NombreArchivo? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(NombreArchivo? left, NombreArchivo? right) => !(left == right);

    public override string ToString() => Value;
}
