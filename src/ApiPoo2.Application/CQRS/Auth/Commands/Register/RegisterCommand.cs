using ApiPoo2.Application.CQRS;
using ApiPoo2.Application.Models;

namespace ApiPoo2.Application.CQRS.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password) : ICommand<UserDto>;