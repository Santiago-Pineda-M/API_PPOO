using ApiPoo2.Application.CQRS;
using ApiPoo2.Application.Models;

namespace ApiPoo2.Application.CQRS.Auth.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery(Guid UserId) : IQuery<UserDto>;