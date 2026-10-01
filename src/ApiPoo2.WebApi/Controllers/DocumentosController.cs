using ApiPoo2.Application.UseCases.Consultas;
using ApiPoo2.Application.UseCases.Documentos;
using ApiPoo2.WebApi.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPoo2.WebApi.Controllers;

[ApiController]
[Route("api/documentos")]
[Authorize(Policy = ApiKeyRequirement.PolicyName)]
public sealed class DocumentosController : ControllerBase
{
    private readonly RegisterDocumentoUseCase _register;
    private readonly GetDocumentoUseCase _get;
    private readonly GetDocumentosUseCase _list;
    private readonly UpdateDocumentoUseCase _update;
    private readonly DeleteDocumentoUseCase _delete;

    public DocumentosController(
        RegisterDocumentoUseCase register,
        GetDocumentoUseCase get,
        GetDocumentosUseCase list,
        UpdateDocumentoUseCase update,
        DeleteDocumentoUseCase delete)
    {
        _register = register;
        _get = get;
        _list = list;
        _update = update;
        _delete = delete;
    }

    [HttpPost]
    public async Task<ActionResult<DocumentoParametricoOutputDto>> Register(
        [FromBody] RegisterDocumentoInputDto request,
        CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, await _register.ExecuteAsync(request, cancellationToken));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentoParametricoOutputDto>>> GetAll(
        CancellationToken cancellationToken)
        => Ok(await _list.ExecuteAsync(new GetDocumentosInputDto(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentoParametricoOutputDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => Ok(await _get.ExecuteAsync(new GetDocumentoInputDto(id), cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DocumentoParametricoOutputDto>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateDocumentoBody body,
        CancellationToken cancellationToken)
        => Ok(await _update.ExecuteAsync(
            new UpdateDocumentoInputDto(
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
        await _delete.ExecuteAsync(new DeleteDocumentoInputDto(id), cancellationToken);
        return NoContent();
    }
}

public sealed record UpdateDocumentoBody(
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
    public async Task<ActionResult<IReadOnlyList<DocumentoVencimientoOutputDto>>> DocumentosPorVencer(
        [FromQuery] int dias,
        CancellationToken cancellationToken)
        => Ok(await _documentosPorVencer.ExecuteAsync(
            new GetDocumentosPorVencerInputDto(dias),
            cancellationToken));

    [HttpGet("documentos-vencidos")]
    public async Task<ActionResult<IReadOnlyList<DocumentoVencimientoOutputDto>>> DocumentosVencidos(
        CancellationToken cancellationToken)
        => Ok(await _documentosVencidos.ExecuteAsync(
            new GetVehiculosDocumentsVencidosInputDto(),
            cancellationToken));

    [HttpGet("personas-por-tipo")]
    public async Task<ActionResult<IReadOnlyList<PersonaCountOutputDto>>> PersonasPorTipo(
        CancellationToken cancellationToken)
        => Ok(await _personasPorTipo.ExecuteAsync(
            new CountPersonasByTipoInputDto(),
            cancellationToken));
}
