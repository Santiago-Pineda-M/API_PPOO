using ApiPoo2.Application.CQRS;
using ApiPoo2.Application.Models;

namespace ApiPoo2.Application.CQRS.Auth.Commands.Logout;

public sealed record LogoutCommand(
    Guid UserId,
    Guid AccessTokenJti,
    DateTime AccessTokenExpiresAtUtc,
    string? RefreshToken) : ICommand<OperationResult>;