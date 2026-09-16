using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Events;

public sealed record PasswordChangedEvent(Guid UserId, DateTime OccurredOnUtc) : IDomainEvent;