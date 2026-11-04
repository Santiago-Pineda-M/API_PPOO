using ApiPoo2.Application.Common;
using ApiPoo2.Application.UseCases.Auth;
using ApiPoo2.Application.UseCases.Usuarios;
using ApiPoo2.Application.UseCases.Usuarios.AsignarRol;
using ApiPoo2.Application.UseCases.Usuarios.Crear;
using ApiPoo2.Application.UseCases.Usuarios.Delete;
using ApiPoo2.Application.UseCases.Usuarios.Get;
using ApiPoo2.Application.UseCases.Usuarios.Listar;
using ApiPoo2.Application.UseCases.Usuarios.Update;
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
    private readonly ListarUsuariosUseCase _listarUsuarios;
    private readonly GetUsuarioUseCase _getUsuario;
    private readonly CrearUsuarioUseCase _crearUsuario;
    private readonly UpdateUsuarioUseCase _actualizarUsuario;
    private readonly DeleteUsuarioUseCase _eliminarUsuario;
    private readonly AsignarRolUseCase _asignarRol;

    public UsuariosController(
        ChangeUserPasswordUseCase changePassword,
        RegenerateApiKeyUseCase regenerateApiKey,
        ListarUsuariosUseCase listarUsuarios,
        GetUsuarioUseCase getUsuario,
        CrearUsuarioUseCase crearUsuario,
        UpdateUsuarioUseCase actualizarUsuario,
        DeleteUsuarioUseCase eliminarUsuario,
        AsignarRolUseCase asignarRol)
    {
        _changePassword = changePassword;
        _regenerateApiKey = regenerateApiKey;
        _listarUsuarios = listarUsuarios;
        _getUsuario = getUsuario;
        _crearUsuario = crearUsuario;
        _actualizarUsuario = actualizarUsuario;
        _eliminarUsuario = eliminarUsuario;
        _asignarRol = asignarRol;
    }

    /// <summary>Listado paginado de usuarios (solo administradores).</summary>
    [Authorize(Roles = "Administrator")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<UsuarioOutputDto>>> Listar(
        [FromQuery] ListarUsuariosInputDto filtro,
        CancellationToken cancellationToken)
        => Ok(await _listarUsuarios.ExecuteAsync(filtro, cancellationToken));

    /// <summary>Detalle de otro usuario por su identificador de persona (solo administradores).</summary>
    [Authorize(Roles = "Administrator")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UsuarioOutputDto>> GetPorId(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => Ok(await _getUsuario.ExecuteAsync(new GetUsuarioInputDto(id), cancellationToken));

    /// <summary>Crea un usuario para una persona existente (solo administradores).</summary>
    [Authorize(Roles = "Administrator")]
    [HttpPost]
    public async Task<ActionResult<UsuarioOutputDto>> Crear(
        [FromBody] CrearUsuarioInputDto request,
        CancellationToken cancellationToken)
        => StatusCode(
            StatusCodes.Status201Created,
            await _crearUsuario.ExecuteAsync(request, cancellationToken));

    /// <summary>Edita el perfil de otro usuario (solo administradores).</summary>
    [Authorize(Roles = "Administrator")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UsuarioOutputDto>> Actualizar(
        [FromRoute] Guid id,
        [FromBody] UpdateUsuarioInputDto request,
        CancellationToken cancellationToken)
    {
        var input = request with { PersonaId = id };
        return Ok(await _actualizarUsuario.ExecuteAsync(input, cancellationToken));
    }

    /// <summary>Elimina (desactiva) a otro usuario (solo administradores).</summary>
    [Authorize(Roles = "Administrator")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await _eliminarUsuario.ExecuteAsync(new DeleteUsuarioInputDto(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Asigna un rol a otro usuario (solo administradores).</summary>
    [Authorize(Roles = "Administrator")]
    [HttpPut("{id:guid}/rol")]
    public async Task<ActionResult<UsuarioOutputDto>> AsignarRol(
        [FromRoute] Guid id,
        [FromBody] AsignarRolInputDto request,
        CancellationToken cancellationToken)
    {
        var input = request with { PersonaId = id };
        return Ok(await _asignarRol.ExecuteAsync(input, cancellationToken));
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
