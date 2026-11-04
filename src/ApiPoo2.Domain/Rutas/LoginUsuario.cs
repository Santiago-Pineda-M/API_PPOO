using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Rutas;

/// <summary>Login del usuario que registra la ruta (se toma del token, nunca del cuerpo).</summary>
public sealed class LoginUsuario
{
    public const int MaxLength = 64;

    public string Value { get; }

    private LoginUsuario(string value) => Value = value;

    public static LoginUsuario From(string value)
    {
        var normalizado = value?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizado))
        {
            throw new DomainValidationException(
                "ruta.login.requerido",
                ["El login del usuario que registra la ruta es obligatorio."]);
        }

        if (normalizado.Length > MaxLength)
        {
            throw new DomainValidationException(
                "ruta.login.longitud",
                [$"El login del usuario no puede superar los {MaxLength} caracteres."]);
        }

        return new LoginUsuario(normalizado);
    }

    public override string ToString() => Value;
}
