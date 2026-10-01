using ApiPoo2.Application.UseCases.Auth;
using ApiPoo2.Application.UseCases.Usuarios;
using ApiPoo2.WebApi.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPoo2.WebApi.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Policy = ApiKeyRequirement.PolicyName)]
public sealed class UsuariosController : ControllerBase
{
    private readonly ChangeUserPasswordUseCase _changePassword;
    private readonly RegenerateApiKeyUseCase _regenerateApiKey;

    public UsuariosController(
        ChangeUserPasswordUseCase changePassword,
        RegenerateApiKeyUseCase regenerateApiKey)
    {
        _changePassword = changePassword;
        _regenerateApiKey = regenerateApiKey;
    }

    /// <summary>La contraseña viaja en el body y el login en la URL, según el enunciado.</summary>
    [HttpPut("{login}/password")]
    public async Task<ActionResult<OperationResult>> ChangePassword(
        [FromRoute] string login,
        [FromBody] ChangeUserPasswordRequest body,
        CancellationToken cancellationToken)
        => Ok(await _changePassword.ExecuteAsync(
            new ChangeUserPasswordInputDto(login, body.NewPassword),
            cancellationToken));

    [HttpGet("{login}/apikey")]
    public async Task<ActionResult<RegenerateApiKeyOutputDto>> RegenerateApiKey(
        [FromRoute] string login,
        CancellationToken cancellationToken)
        => Ok(await _regenerateApiKey.ExecuteAsync(new RegenerateApiKeyInputDto(login), cancellationToken));
}

public sealed record ChangeUserPasswordRequest(string NewPassword);
