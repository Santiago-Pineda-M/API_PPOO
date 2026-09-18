namespace ApiPoo2.Application.DTOs;

public sealed record OperationResult(bool Succeeded = true)
{
    public static OperationResult Success() => new();
}