using ApiPoo2.Domain.Exceptions;

namespace ApiPoo2.Domain.ValueObjects;

public sealed record PasswordHash
{
    public const string DefaultAlgorithm = "BCRYPT";

    public string Algorithm { get; }

    public string Hash { get; }

    private PasswordHash(string algorithm, string hash)
    {
        Algorithm = algorithm;
        Hash = hash;
    }

    public static PasswordHash Create(string hash, string algorithm = DefaultAlgorithm)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            throw new DomainValidationException("password.invalid", ["El hash de la contraseña es obligatorio."]);
        }

        return new PasswordHash(algorithm, hash);
    }
}