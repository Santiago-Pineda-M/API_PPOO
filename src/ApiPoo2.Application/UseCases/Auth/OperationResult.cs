namespace ApiPoo2.Application.UseCases.Auth;

public sealed record OperationResult(bool Succeeded = true)
{
    public static OperationResult Success() => new();
}
