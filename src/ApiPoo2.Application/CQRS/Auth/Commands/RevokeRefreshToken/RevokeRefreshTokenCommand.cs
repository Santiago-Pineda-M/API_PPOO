using ApiPoo2.Application.CQRS;
using ApiPoo2.Application.Models;

namespace ApiPoo2.Application.CQRS.Auth.Commands.RevokeRefreshToken;

public sealed record RevokeRefreshTokenCommand(Guid UserId, string RefreshToken) : ICommand<OperationResult>;