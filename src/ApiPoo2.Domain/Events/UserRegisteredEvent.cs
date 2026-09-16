using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Events;

public sealed record UserRegisteredEvent(Guid UserId, string Email, DateTime OccurredOnUtc) : IDomainEvent;