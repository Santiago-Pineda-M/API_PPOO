namespace ApiPoo2.Application.Exceptions;

public abstract class BaseApplicationException : Exception
{
    public int HttpStatusCode { get; }

    public string Code { get; }

    protected BaseApplicationException(int httpStatusCode, string code, string message)
        : base(message)
    {
        HttpStatusCode = httpStatusCode;
        Code = code;
    }
}