namespace ApiPoo2.Domain.Exceptions;

public class DomainValidationException : DomainException
{
    public IReadOnlyList<string> Errors { get; }

    public DomainValidationException(string code, IEnumerable<string> errors)
        : base(code, errors.FirstOrDefault() ?? "La operación no superó la validación de dominio.")
    {
        Errors = [.. errors];
    }
}