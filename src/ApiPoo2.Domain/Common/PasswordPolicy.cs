using ApiPoo2.Domain.Exceptions;

namespace ApiPoo2.Domain.Common;

public static class PasswordPolicy
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 128;

    public static void EnsureValid(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(password))
        {
            errors.Add("La contraseña es obligatoria.");
        }
        else
        {
            if (password.Length < MinimumLength)
            {
                errors.Add($"La contraseña debe tener al menos {MinimumLength} caracteres.");
            }

            if (password.Length > MaximumLength)
            {
                errors.Add($"La contraseña no puede superar los {MaximumLength} caracteres.");
            }

            if (!password.Any(char.IsUpper))
            {
                errors.Add("La contraseña debe contener al menos una letra mayúscula.");
            }

            if (!password.Any(char.IsLower))
            {
                errors.Add("La contraseña debe contener al menos una letra minúscula.");
            }

            if (!password.Any(char.IsDigit))
            {
                errors.Add("La contraseña debe contener al menos un número.");
            }

            if (password.All(char.IsLetterOrDigit))
            {
                errors.Add("La contraseña debe contener al menos un carácter especial.");
            }
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException("password.policy", errors);
        }
    }
}