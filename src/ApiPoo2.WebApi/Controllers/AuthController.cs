using ApiPoo2.Application.UseCases.Auth;
using ApiPoo2.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPoo2.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly LoginUseCase _login;
    private readonly RefreshTokenUseCase _refreshToken;
    private readonly LogoutUseCase _logout;
    private readonly RevokeRefreshTokenUseCase _revokeRefreshToken;
    private readonly ChangePasswordUseCase _changePassword;

    public AuthController(
        LoginUseCase login,
        RefreshTokenUseCase refreshToken,
        LogoutUseCase logout,
        RevokeRefreshTokenUseCase revokeRefreshToken,
        ChangePasswordUseCase changePassword)
    {
        _login = login;
        _refreshToken = refreshToken;
        _logout = logout;
        _revokeRefreshToken = revokeRefreshToken;
        _changePassword = changePassword;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginTokenPairOutputDto>> Login(
        [FromBody] LoginInputDto request,
        CancellationToken cancellationToken)
        => Ok(await _login.ExecuteAsync(request, cancellationToken));

    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshTokenPairOutputDto>> Refresh(
        [FromBody] RefreshTokenInputDto request,
        CancellationToken cancellationToken)
        => Ok(await _refreshToken.ExecuteAsync(request, cancellationToken));

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutInputDto? body, CancellationToken cancellationToken)
    {
        await _logout.ExecuteAsync(
            new LogoutInputDto(
                User.GetPersonaId(),
                User.GetTokenJti(),
                User.GetTokenExpiresAtUtc(),
                body?.RefreshToken),
            cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPost("revoke-token")]
    public async Task<IActionResult> RevokeToken(
        [FromBody] RevokeRefreshTokenInputDto body,
        CancellationToken cancellationToken)
    {
        await _revokeRefreshToken.ExecuteAsync(
            new RevokeRefreshTokenInputDto(User.GetPersonaId(), body.RefreshToken),
            cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordInputDto body,
        CancellationToken cancellationToken)
    {
        await _changePassword.ExecuteAsync(
            new ChangePasswordInputDto(
                User.GetPersonaId(),
                body.CurrentPassword,
                body.NewPassword,
                User.GetTokenJti(),
                User.GetTokenExpiresAtUtc()),
            cancellationToken);

        return NoContent();
    }
}
