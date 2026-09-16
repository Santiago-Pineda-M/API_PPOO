using System.Text.RegularExpressions;
using ApiPoo2.Domain.Exceptions;

namespace ApiPoo2.Domain.ValueObjects;

public sealed record Email
{
    private static readonly Regex Pattern =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public const int MaxLength = 320;

    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email From(string value)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > MaxLength || !Pattern.IsMatch(normalized))
        {
            throw new DomainValidationException("email.invalid", ["El correo electrónico no es válido."]);
        }

        return new Email(normalized.ToLowerInvariant());
    }

    public override string ToString() => Value;
}