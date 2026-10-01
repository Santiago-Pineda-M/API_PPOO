using System.Text;
using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.TiposDocumento;

/// <summary>
///     Contenido binario del documento. El enunciado pide BLOB y Base64; acá se guardan los bytes
///     reales del PDF y la capa Application se encarga de codificar y decodificar el Base64.
/// </summary>
public sealed class ContenidoDocumento
{
    public const string ContentTypePorDefecto = "application/pdf";

    private readonly byte[] _value;

    private ContenidoDocumento(byte[] value, string contentType)
    {
        _value = value;
        ContentType = contentType;
    }

    public int Length => _value.Length;

    public string ContentType { get; }

    public byte[] ToArray() => (byte[])_value.Clone();

    public string ToBase64() => Convert.ToBase64String(_value);

    public static ContenidoDocumento From(byte[] contenido, string nombreArchivo, string? contentType = null)
    {
        ArgumentNullException.ThrowIfNull(contenido);

        var errors = new List<string>();

        if (contenido.Length == 0)
        {
            errors.Add("El contenido del documento no puede estar vacío.");
        }

        var normalizado = Normalize(nombreArchivo);

        if (!EsPdf(contenido, normalizado))
        {
            errors.Add("Solo se aceptan documentos en formato PDF.");
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException("documento.contenido", errors);
        }

        var tipo = string.IsNullOrWhiteSpace(contentType) ? ContentTypePorDefecto : contentType;

        return new ContenidoDocumento((byte[])contenido.Clone(), tipo);
    }

    public static ContenidoDocumento FromBase64(string base64, string nombreArchivo, string? contentType = null)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            throw new DomainValidationException(
                "documento.contenido",
                ["El contenido Base64 del documento es obligatorio."]);
        }

        byte[] decoded;

        try
        {
            decoded = Convert.FromBase64String(base64.Trim());
        }
        catch (FormatException)
        {
            throw new DomainValidationException(
                "documento.contenido",
                ["El contenido enviado no es un Base64 válido."]);
        }

        return From(decoded, nombreArchivo, contentType);
    }

    private static string Normalize(string nombreArchivo) => nombreArchivo?.Trim().ToUpperInvariant() ?? string.Empty;

    private static bool EsPdf(byte[] contenido, string nombreArchivo)
    {
        if (nombreArchivo.EndsWith(".PDF", StringComparison.Ordinal))
        {
            return true;
        }

        var firma = "%PDF";

        if (contenido.Length < firma.Length)
        {
            return false;
        }

        return Encoding.ASCII.GetString(contenido, 0, firma.Length).Equals(firma, StringComparison.Ordinal);
    }
}
