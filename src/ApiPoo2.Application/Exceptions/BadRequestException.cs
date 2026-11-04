namespace ApiPoo2.Application.Exceptions;

/// <summary>
///     Error de solicitud con un código de negocio propio (por ejemplo, un archivo de cargue con
///     formato inválido). A diferencia de <see cref="RequestValidationException" />, conserva el
///     código específico del caso de uso y puede transportar la lista de errores.
/// </summary>
public sealed class BadRequestException : BaseApplicationException
{
    public IReadOnlyList<string> Errors { get; }

    public BadRequestException(string code, string message, IEnumerable<string>? errors = null)
        : base(400, code, message)
    {
        Errors = errors is null ? [] : [.. errors];
    }
}
