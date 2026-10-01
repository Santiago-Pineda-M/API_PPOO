using System.Text;
using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Personas;

/// <summary>
///     Login del usuario. Sigue la regla mnemotécnica del enunciado: primera letra del nombre,
///     primera letra del apellido y el número de identificación.
/// </summary>
public sealed class Login
{
    public const int MaxLength = 64;

    public string Value { get; }

    private Login(string value) => Value = value;

    public static Login From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException(
                "user.login",
                ["El login es obligatorio."]);
        }

        if (value.Length > MaxLength)
        {
            throw new DomainValidationException(
                "user.login",
                [$"El login no puede superar los {MaxLength} caracteres."]);
        }

        if (!IsValid(value))
        {
            throw new DomainValidationException(
                "user.login",
                ["El login solo puede contener letras, dígitos y guion bajo."]);
        }

        return new Login(value);
    }

    /// <summary>
    ///     Genera el login mnemotécnico. <paramref name="sufix" /> resuelve homónimos: dos
    ///     "Juan Pérez" con el mismo documento producirían el mismo login base.
    /// </summary>
    public static Login FromMnemonic(string nombres, string apellidos, NumeroIdentificacion numero, int sufijo = 0)
    {
        var iniciales = FirstLetter(nombres) + FirstLetter(apellidos) + numero.Value;

        var value = sufijo <= 0
            ? iniciales
            : iniciales + "_" + sufijo;

        return From(value);
    }

    private static string FirstLetter(string value)
    {
        foreach (var character in value)
        {
            if (char.IsLetter(character))
            {
                return char.ToUpperInvariant(character).ToString();
            }
        }

        return string.Empty;
    }

    private static bool IsValid(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                builder.Append(character);
            }
        }

        return builder.Length == value.Length;
    }

    public bool Equals(Login? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is Login otro && Equals(otro);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(Login? left, Login? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Login? left, Login? right) => !(left == right);

    public override string ToString() => Value;
}
