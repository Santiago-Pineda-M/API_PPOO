using ApiPoo2.Application.DTOs;
using ApiPoo2.Application.UseCases.Auth.ChangePassword;
using ApiPoo2.Application.UseCases.Auth.Login;
using ApiPoo2.Application.UseCases.Auth.Logout;
using ApiPoo2.Application.UseCases.Auth.RefreshToken;
using ApiPoo2.Application.UseCases.Auth.Register;
using ApiPoo2.Application.UseCases.Auth.RevokeRefreshToken;
using ApiPoo2.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPoo2.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly RegisterUseCase _register;
    private readonly LoginUseCase _login;
    private readonly RefreshTokenUseCase _refreshToken;
    private readonly LogoutUseCase _logout;
    private readonly RevokeRefreshTokenUseCase _revokeRefreshToken;
    private readonly ChangePasswordUseCase _changePassword;

    public AuthController(
        RegisterUseCase register,
        LoginUseCase login,
        RefreshTokenUseCase refreshToken,
        LogoutUseCase logout,
        RevokeRefreshTokenUseCase revokeRefreshToken,
        ChangePasswordUseCase changePassword)
    {
        _register = register;
        _login = login;
        _refreshToken = refreshToken;
        _logout = logout;
        _revokeRefreshToken = revokeRefreshToken;
        _changePassword = changePassword;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterUserDto>> Register([FromBody] RegisterInputDto request, CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, await _register.ExecuteAsync(request, cancellationToken));

    [HttpPost("login")]
    public async Task<ActionResult<LoginTokenPairDto>> Login([FromBody] LoginInputDto request, CancellationToken cancellationToken)
        => Ok(await _login.ExecuteAsync(request, cancellationToken));

    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshTokenPairDto>> Refresh([FromBody] RefreshTokenInputDto request, CancellationToken cancellationToken)
        => Ok(await _refreshToken.ExecuteAsync(request, cancellationToken));

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutInputDto? body, CancellationToken cancellationToken)
    {
        var request = new LogoutInputDto(
            User.GetUserId(),
            User.GetTokenJti(),
            User.GetTokenExpiresAtUtc(),
            body?.RefreshToken);

        await _logout.ExecuteAsync(request, cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPost("revoke-token")]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeRefreshTokenInputDto body, CancellationToken cancellationToken)
    {
        await _revokeRefreshToken.ExecuteAsync(
            new RevokeRefreshTokenInputDto(User.GetUserId(), body.RefreshToken),
            cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordInputDto body, CancellationToken cancellationToken)
    {
        await _changePassword.ExecuteAsync(
            new ChangePasswordInputDto(
                User.GetUserId(),
                body.CurrentPassword,
                body.NewPassword,
                User.GetTokenJti(),
                User.GetTokenExpiresAtUtc()),
            cancellationToken);

        return NoContent();
    }
}