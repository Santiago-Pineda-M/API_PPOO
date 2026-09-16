namespace ApiPoo2.Application.Exceptions;

public sealed class ConflictException : BaseApplicationException
{
    public ConflictException(string code, string message)
        : base(409, code, message)
    {
    }
}