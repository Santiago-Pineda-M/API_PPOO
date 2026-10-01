using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Personas;

/// <summary>Correo electrónico de la persona. Validación RFC 5322 simplificada.</summary>
public sealed class CorreoElectronico
{
    public const int MaxLength = 320;

    public string Value { get; }

    private CorreoElectronico(string value) => Value = value;

    public static CorreoElectronico From(string value)
    {
        var errors = new List<string>();
        var normalizado = value?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizado))
        {
            errors.Add("El correo electrónico es obligatorio.");
        }
        else if (normalizado.Length > MaxLength)
        {
            errors.Add($"El correo no puede superar los {MaxLength} caracteres.");
        }
        else if (!EsFormatoValido(normalizado))
        {
            errors.Add("El formato del correo electrónico no es válido.");
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException("persona.correo", errors);
        }

        return new CorreoElectronico(normalizado);
    }

    private static bool EsFormatoValido(string email)
    {
        // RFC 5322 simplificado
        var parts = email.Split('@');
        if (parts.Length != 2) return false;
        var local = parts[0];
        var domain = parts[1];
        if (string.IsNullOrWhiteSpace(local) || local.Length > 64) return false;
        if (string.IsNullOrWhiteSpace(domain) || domain.Length > 255) return false;
        if (!domain.Contains('.')) return false;
        if (domain.StartsWith('.') || domain.EndsWith('.')) return false;
        return true;
    }

    public bool Equals(CorreoElectronico? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is CorreoElectronico otro && Equals(otro);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(CorreoElectronico? left, CorreoElectronico? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(CorreoElectronico? left, CorreoElectronico? right)
        => !(left == right);

    public override string ToString() => Value;
}