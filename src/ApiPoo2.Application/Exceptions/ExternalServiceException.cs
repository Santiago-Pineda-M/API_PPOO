namespace ApiPoo2.Application.Exceptions;

public sealed class ExternalServiceException : BaseApplicationException
{
    public ExternalServiceException(string code, string message)
        : base(502, code, message)
    {
    }
}
