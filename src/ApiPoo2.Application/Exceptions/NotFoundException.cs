namespace ApiPoo2.Application.Exceptions;

public sealed class NotFoundException : BaseApplicationException
{
    public NotFoundException(string code, string message)
        : base(404, code, message)
    {
    }
}