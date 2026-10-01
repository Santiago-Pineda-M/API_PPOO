using ApiPoo2.Application.UseCases.Consultas;
using ApiPoo2.Application.UseCases.TiposDocumento;
using ApiPoo2.WebApi.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPoo2.WebApi.Controllers;

[ApiController]
[Route("api/tipos-documento")]
[Authorize(Policy = ApiKeyRequirement.PolicyName)]
public sealed class TiposDocumentoController : ControllerBase
{
    private readonly RegisterTipoDocumentoUseCase _register;
    private readonly GetTipoDocumentoUseCase _get;
    private readonly GetTiposDocumentoUseCase _list;
    private readonly UpdateTipoDocumentoUseCase _update;
    private readonly DeleteTipoDocumentoUseCase _delete;

    public TiposDocumentoController(
        RegisterTipoDocumentoUseCase register,
        GetTipoDocumentoUseCase get,
        GetTiposDocumentoUseCase list,
        UpdateTipoDocumentoUseCase update,
        DeleteTipoDocumentoUseCase delete)
    {
        _register = register;
        _get = get;
        _list = list;
        _update = update;
        _delete = delete;
    }

    [HttpPost]
    public async Task<ActionResult<TipoDocumentoOutputDto>> Register(
        [FromBody] RegisterTipoDocumentoInputDto request,
        CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, await _register.ExecuteAsync(request, cancellationToken));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TipoDocumentoOutputDto>>> GetAll(
        CancellationToken cancellationToken)
        => Ok(await _list.ExecuteAsync(new GetTiposDocumentoInputDto(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TipoDocumentoOutputDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => Ok(await _get.ExecuteAsync(new GetTipoDocumentoInputDto(id), cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TipoDocumentoOutputDto>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateTipoDocumentoBody body,
        CancellationToken cancellationToken)
        => Ok(await _update.ExecuteAsync(
            new UpdateTipoDocumentoInputDto(
                id,
                body.Nombre,
                body.TiposVehiculoAplicables,
                body.CodigoObligatoriedad,
                body.Descripcion),
            cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await _delete.ExecuteAsync(new DeleteTipoDocumentoInputDto(id), cancellationToken);
        return NoContent();
    }
}

public sealed record UpdateTipoDocumentoBody(
    string Nombre,
    string TiposVehiculoAplicables,
    string CodigoObligatoriedad,
    string Descripcion);

[ApiController]
[Route("api/consultas")]
[AllowAnonymous]
public sealed class ConsultasController : ControllerBase
{
    private readonly GetConductoresOperablesUseCase _conductoresOperables;
    private readonly GetDocumentosPorVencerUseCase _documentosPorVencer;
    private readonly GetVehiculosDocumentosVencidosUseCase _documentosVencidos;
    private readonly CountPersonasByTipoUseCase _personasPorTipo;

    public ConsultasController(
        GetConductoresOperablesUseCase conductoresOperables,
        GetDocumentosPorVencerUseCase documentosPorVencer,
        GetVehiculosDocumentosVencidosUseCase documentosVencidos,
        CountPersonasByTipoUseCase personasPorTipo)
    {
        _conductoresOperables = conductoresOperables;
        _documentosPorVencer = documentosPorVencer;
        _documentosVencidos = documentosVencidos;
        _personasPorTipo = personasPorTipo;
    }

    [HttpGet("conductores-operables")]
    public async Task<ActionResult<IReadOnlyList<ConductorOperableOutputDto>>> ConductoresOperables(
        [FromQuery] int cantidad = 50,
        CancellationToken cancellationToken = default)
        => Ok(await _conductoresOperables.ExecuteAsync(
            new GetConductoresOperablesInputDto(cantidad),
            cancellationToken));

    [HttpGet("documentos-por-vencer")]
    public async Task<ActionResult<IReadOnlyList<DocumentoVehiculoVencimientoOutputDto>>> DocumentosPorVencer(
        [FromQuery] int dias,
        CancellationToken cancellationToken)
        => Ok(await _documentosPorVencer.ExecuteAsync(
            new GetDocumentosPorVencerInputDto(dias),
            cancellationToken));

    [HttpGet("documentos-vencidos")]
    public async Task<ActionResult<IReadOnlyList<DocumentoVehiculoVencimientoOutputDto>>> DocumentosVencidos(
        CancellationToken cancellationToken)
        => Ok(await _documentosVencidos.ExecuteAsync(
            new GetVehiculosDocumentosVencidosInputDto(),
            cancellationToken));

    [HttpGet("personas-por-tipo")]
    public async Task<ActionResult<IReadOnlyList<PersonaCountOutputDto>>> PersonasPorTipo(
        CancellationToken cancellationToken)
        => Ok(await _personasPorTipo.ExecuteAsync(
            new CountPersonasByTipoInputDto(),
            cancellationToken));
}
