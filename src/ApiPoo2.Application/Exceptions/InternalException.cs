namespace ApiPoo2.Application.Exceptions;

public sealed class InternalException : BaseApplicationException
{
    public InternalException(string code, string message)
        : base(500, code, message)
    {
    }
}
