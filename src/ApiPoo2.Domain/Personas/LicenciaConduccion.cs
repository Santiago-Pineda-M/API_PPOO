using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Personas;

/// <summary>
///     Licencia de conducción del conductor. El enunciado pide BLOB y Base64: la API recibe el
///     documento en Base64, el dominio valida que sea un PDF y la infraestructura guarda los bytes.
/// </summary>
public sealed class LicenciaConduccion
{
    public const int MaxBytes = 5 * 1024 * 1024;

    private readonly byte[] _value;

    private LicenciaConduccion(byte[] value) => _value = value;

    public int Length => _value.Length;

    public byte[] ToArray() => (byte[])_value.Clone();

    public static LicenciaConduccion From(byte[] contenido)
    {
        ArgumentNullException.ThrowIfNull(contenido);

        if (contenido.Length == 0)
        {
            throw new DomainValidationException(
                "persona.licencia.formato",
                ["La licencia de conducción no puede estar vacía."]);
        }

        if (contenido.Length > MaxBytes)
        {
            throw new DomainValidationException(
                "persona.licencia.formato",
                [$"La licencia de conducción no puede superar los {MaxBytes / (1024 * 1024)} MB."]);
        }

        if (!EsPdf(contenido))
        {
            throw new DomainValidationException(
                "persona.licencia.formato",
                ["Solo se aceptan licencias de conducción en formato PDF."]);
        }

        return new LicenciaConduccion((byte[])contenido.Clone());
    }

    public static LicenciaConduccion FromBase64(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            throw new DomainValidationException(
                "persona.licencia.formato",
                ["El contenido Base64 de la licencia es obligatorio."]);
        }

        try
        {
            return From(Convert.FromBase64String(base64.Trim()));
        }
        catch (FormatException)
        {
            throw new DomainValidationException(
                "persona.licencia.formato",
                ["El contenido enviado no es un Base64 válido."]);
        }
    }

    private static bool EsPdf(byte[] contenido)
    {
        ReadOnlySpan<byte> firma = "%PDF"u8;

        return contenido.Length >= firma.Length
            && contenido.AsSpan(0, firma.Length).SequenceEqual(firma);
    }
}
