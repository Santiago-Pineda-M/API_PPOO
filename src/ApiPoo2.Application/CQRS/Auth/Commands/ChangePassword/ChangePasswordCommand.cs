using ApiPoo2.Application.CQRS;
using ApiPoo2.Application.Models;

namespace ApiPoo2.Application.CQRS.Auth.Commands.ChangePassword;

public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : ICommand<OperationResult>;