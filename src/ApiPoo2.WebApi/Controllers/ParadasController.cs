using ApiPoo2.Application.UseCases.Paradas.Delete;
using ApiPoo2.Application.UseCases.Paradas.Get;
using ApiPoo2.Application.UseCases.Paradas.Update;
using ApiPoo2.Application.UseCases.Rutas;
using ApiPoo2.WebApi.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPoo2.WebApi.Controllers;

/// <summary>
///     La consulta de una parada es una lectura del enunciado de E3 (solo token); las escrituras
///     mantienen token + APIKey.
/// </summary>
[ApiController]
[Route("api/paradas")]
[Authorize]
public sealed class ParadasController : ControllerBase
{
    private readonly GetParadaUseCase _get;
    private readonly UpdateParadaUseCase _update;
    private readonly DeleteParadaUseCase _delete;

    public ParadasController(
        GetParadaUseCase get,
        UpdateParadaUseCase update,
        DeleteParadaUseCase delete)
    {
        _get = get;
        _update = update;
        _delete = delete;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ParadaOutputDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => Ok(await _get.ExecuteAsync(new GetParadaInputDto(id), cancellationToken));

    [Authorize(Policy = ApiKeyRequirement.PolicyName)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ParadaOutputDto>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateParadaBody body,
        CancellationToken cancellationToken)
        => Ok(await _update.ExecuteAsync(
            new UpdateParadaInputDto(
                id,
                body.Ubicacion,
                body.OrdenParada,
                body.Latitud,
                body.Longitud),
            cancellationToken));

    [Authorize(Policy = ApiKeyRequirement.PolicyName)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await _delete.ExecuteAsync(new DeleteParadaInputDto(id), cancellationToken);
        return NoContent();
    }
}

public sealed record UpdateParadaBody(
    string Ubicacion,
    int OrdenParada,
    double? Latitud,
    double? Longitud);
