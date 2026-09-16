namespace ApiPoo2.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}