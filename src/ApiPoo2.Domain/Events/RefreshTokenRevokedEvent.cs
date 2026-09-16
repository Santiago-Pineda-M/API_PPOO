using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Enums;

namespace ApiPoo2.Domain.Events;

public sealed record RefreshTokenRevokedEvent(Guid UserId, Guid RefreshTokenId, RevocationReason Reason, DateTime OccurredOnUtc) : IDomainEvent;