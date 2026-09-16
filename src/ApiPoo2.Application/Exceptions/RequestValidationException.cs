namespace ApiPoo2.Application.Exceptions;

public sealed class RequestValidationException : BaseApplicationException
{
    public IReadOnlyList<string> Errors { get; }

    public RequestValidationException(IEnumerable<string> errors)
        : base(400, "validation.failed", "La solicitud contiene datos inválidos.")
    {
        Errors = [.. errors];
    }
}