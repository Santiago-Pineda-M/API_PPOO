using ApiPoo2.Application.CQRS;
using ApiPoo2.Application.Models;

namespace ApiPoo2.Application.CQRS.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<TokenPairDto>;