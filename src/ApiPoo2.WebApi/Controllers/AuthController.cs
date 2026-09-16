using ApiPoo2.Application.Models;
using ApiPoo2.Application.CQRS;
using ApiPoo2.Application.CQRS.Auth.Commands.ChangePassword;
using ApiPoo2.Application.CQRS.Auth.Commands.Login;
using ApiPoo2.Application.CQRS.Auth.Commands.Logout;
using ApiPoo2.Application.CQRS.Auth.Commands.RefreshToken;
using ApiPoo2.Application.CQRS.Auth.Commands.Register;
using ApiPoo2.Application.CQRS.Auth.Commands.RevokeRefreshToken;
using ApiPoo2.WebApi.Contracts.Requests;
using ApiPoo2.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPoo2.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public AuthController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.SendAsync<RegisterCommand, UserDto>(
            new RegisterCommand(request.Email, request.Password),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenPairDto>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.SendAsync<LoginCommand, TokenPairDto>(
            new LoginCommand(request.Email, request.Password),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenPairDto>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.SendAsync<RefreshTokenCommand, TokenPairDto>(
            new RefreshTokenCommand(request.RefreshToken),
            cancellationToken);

        return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync<LogoutCommand, OperationResult>(
            new LogoutCommand(
                User.GetUserId(),
                User.GetTokenJti(),
                User.GetTokenExpiresAtUtc(),
                request?.RefreshToken),
            cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPost("revoke-token")]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync<RevokeRefreshTokenCommand, OperationResult>(
            new RevokeRefreshTokenCommand(User.GetUserId(), request.RefreshToken),
            cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync<ChangePasswordCommand, OperationResult>(
            new ChangePasswordCommand(User.GetUserId(), request.CurrentPassword, request.NewPassword),
            cancellationToken);

        return NoContent();
    }
}