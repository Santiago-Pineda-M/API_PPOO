using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Events;

public sealed record UserLockedOutEvent(Guid UserId, DateTimeOffset LockoutEndUtc, DateTime OccurredOnUtc) : IDomainEvent;