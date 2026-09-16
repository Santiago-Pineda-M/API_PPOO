namespace ApiPoo2.Application.Models;

public sealed record OperationResult(bool Succeeded = true)
{
    public static OperationResult Success() => new();
}