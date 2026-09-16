namespace ApiPoo2.Application.Exceptions;

public sealed class ForbiddenException : BaseApplicationException
{
    public ForbiddenException(string code, string message)
        : base(403, code, message)
    {
    }
}