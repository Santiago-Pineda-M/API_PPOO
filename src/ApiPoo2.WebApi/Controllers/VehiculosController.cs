using ApiPoo2.Application.UseCases.Auth;
using ApiPoo2.Application.UseCases.Documentos;
using ApiPoo2.Application.UseCases.Vehiculos;
using ApiPoo2.Application.UseCases.Vehiculos.Consultas;
using ApiPoo2.Application.UseCases.Vehiculos.Delete;
using ApiPoo2.Application.UseCases.Vehiculos.Update;
using ApiPoo2.Domain.Documentos;
using ApiPoo2.Domain.Vehiculos;
using ApiPoo2.WebApi.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPoo2.WebApi.Controllers;

[ApiController]
[Route("api/vehiculos")]
public sealed class VehiculosController : ControllerBase
{
    private readonly CreateVehiculoUseCase _create;
    private readonly GetVehiculoByPlacaUseCase _getByPlaca;
    private readonly GetVehiculoByIdUseCase _getById;
    private readonly UpdateVehiculoUseCase _update;
    private readonly DeleteVehiculoUseCase _delete;
    private readonly GetVehiculosByTipoUseCase _byTipo;
    private readonly GetVehiculosByDocumentoUseCase _byDocumento;
    private readonly GetVehiculosByEstadoDocumentoUseCase _byEstado;
    private readonly ChangeDocumentoEstadoUseCase _cambiarEstadoDocumento;
    private readonly UploadDocumentosUseCase _upload;

    public VehiculosController(
        CreateVehiculoUseCase create,
        GetVehiculoByPlacaUseCase getByPlaca,
        GetVehiculoByIdUseCase getById,
        UpdateVehiculoUseCase update,
        DeleteVehiculoUseCase delete,
        GetVehiculosByTipoUseCase byTipo,
        GetVehiculosByDocumentoUseCase byDocumento,
        GetVehiculosByEstadoDocumentoUseCase byEstado,
        ChangeDocumentoEstadoUseCase cambiarEstadoDocumento,
        UploadDocumentosUseCase upload)
    {
        _create = create;
        _getByPlaca = getByPlaca;
        _getById = getById;
        _update = update;
        _delete = delete;
        _byTipo = byTipo;
        _byDocumento = byDocumento;
        _byEstado = byEstado;
        _cambiarEstadoDocumento = cambiarEstadoDocumento;
        _upload = upload;
    }

    /// <summary>Consulta pública: no requiere token.</summary>
    [AllowAnonymous]
    [HttpGet("placa/{placa}")]
    public async Task<ActionResult<VehiculoConsultaOutputDto>> GetByPlaca(
        [FromRoute] string placa,
        CancellationToken cancellationToken)
        => Ok(await _getByPlaca.ExecuteAsync(new GetVehiculoByPlacaInputDto(placa), cancellationToken));

    [Authorize(Policy = ApiKeyRequirement.PolicyName)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VehiculoConsultaOutputDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => Ok(await _getById.ExecuteAsync(new GetVehiculoByIdInputDto(id), cancellationToken));

    [Authorize(Policy = ApiKeyRequirement.PolicyName)]
    [HttpGet("tipo/{tipoVehiculo}")]
    public async Task<ActionResult<IReadOnlyList<VehiculoResumenOutputDto>>> GetByTipo(
        [FromRoute] TipoVehiculo tipoVehiculo,
        CancellationToken cancellationToken)
        => Ok(await _byTipo.ExecuteAsync(new GetVehiculosByTipoInputDto(tipoVehiculo), cancellationToken));

    [Authorize(Policy = ApiKeyRequirement.PolicyName)]
    [HttpGet("documento/{codigo}")]
    public async Task<ActionResult<IReadOnlyList<VehiculoResumenOutputDto>>> GetByDocumento(
        [FromRoute] string codigo,
        CancellationToken cancellationToken)
        => Ok(await _byDocumento.ExecuteAsync(new GetVehiculosByDocumentoInputDto(codigo), cancellationToken));

    [Authorize(Policy = ApiKeyRequirement.PolicyName)]
    [HttpGet("estado-documento/{estado}")]
    public async Task<ActionResult<IReadOnlyList<VehiculoResumenOutputDto>>> GetByEstadoDocumento(
        [FromRoute] EstadoDocumento estado,
        CancellationToken cancellationToken)
        => Ok(await _byEstado.ExecuteAsync(new GetVehiculosByEstadoDocumentoInputDto(estado), cancellationToken));

    [Authorize(Policy = ApiKeyRequirement.PolicyName)]
    [HttpPost]
    public async Task<ActionResult<CreateVehiculoOutputDto>> Create(
        [FromBody] CreateVehiculoInputDto request,
        CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, await _create.ExecuteAsync(request, cancellationToken));

    [Authorize(Policy = ApiKeyRequirement.PolicyName)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UpdateVehiculoOutputDto>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateVehiculoBody body,
        CancellationToken cancellationToken)
        => Ok(await _update.ExecuteAsync(
            new UpdateVehiculoInputDto(
                id,
                body.Placa,
                body.TipoVehiculo,
                body.TipoServicio,
                body.TipoCombustible,
                body.CapacidadPasajeros,
                body.Color,
                body.Modelo,
                body.Marca,
                body.Linea),
            cancellationToken));

    [Authorize(Policy = ApiKeyRequirement.PolicyName)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await _delete.ExecuteAsync(new DeleteVehiculoInputDto(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Cambia el estado almacenado de un documento asociado.</summary>
    [Authorize(Policy = ApiKeyRequirement.PolicyName)]
    [HttpPut("{vehiculoId:guid}/documentos/{documentoId:guid}/estado")]
    public async Task<ActionResult<OperationResult>> CambiarEstadoDocumento(
        [FromRoute] Guid vehiculoId,
        [FromRoute] Guid documentoId,
        [FromBody] ChangeDocumentoEstadoBody body,
        CancellationToken cancellationToken)
        => Ok(await _cambiarEstadoDocumento.ExecuteAsync(
            new ChangeDocumentoEstadoInputDto(vehiculoId, documentoId, body.Estado),
            cancellationToken));

    /// <summary>Cargue y/o actualización de uno o varios documentos en Base64.</summary>
    [Authorize(Policy = ApiKeyRequirement.PolicyName)]
    [HttpPost("{vehiculoId:guid}/documentos")]
    public async Task<ActionResult<DocumentoUploadOutputDto>> UploadDocumentos(
        [FromRoute] Guid vehiculoId,
        [FromBody] UploadDocumentosRequest body,
        CancellationToken cancellationToken)
        => Ok(await _upload.ExecuteAsync(
            new UploadDocumentosInputDto(vehiculoId, body.Documentos),
            cancellationToken));
}

public sealed record UpdateVehiculoBody(
    string Placa,
    TipoVehiculo TipoVehiculo,
    TipoServicio TipoServicio,
    TipoCombustible TipoCombustible,
    int CapacidadPasajeros,
    string Color,
    int Modelo,
    string Marca,
    string Linea);

public sealed record ChangeDocumentoEstadoBody(EstadoDocumento Estado);

public sealed record UploadDocumentosRequest(IReadOnlyList<DocumentoUploadItem> Documentos);
