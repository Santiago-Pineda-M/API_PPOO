namespace ApiPoo2.Application.Exceptions;

public sealed class UnauthorizedException : BaseApplicationException
{
    public UnauthorizedException(string code, string message)
        : base(401, code, message)
    {
    }
}