using ApiPoo2.Application.CQRS;
using ApiPoo2.Application.Models;

namespace ApiPoo2.Application.CQRS.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<TokenPairDto>;