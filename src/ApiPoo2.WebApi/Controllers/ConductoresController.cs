using ApiPoo2.Application.UseCases.Auth;
using ApiPoo2.Application.UseCases.Conductores;
using ApiPoo2.Domain.Personas;
using ApiPoo2.WebApi.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPoo2.WebApi.Controllers;

[ApiController]
[Route("api/conductores")]
[Authorize(Policy = ApiKeyRequirement.PolicyName)]
public sealed class ConductoresController : ControllerBase
{
    private readonly AssociateVehiculosUseCase _associate;
    private readonly ChangeConductorEstadoUseCase _changeState;

    public ConductoresController(
        AssociateVehiculosUseCase associate,
        ChangeConductorEstadoUseCase changeState)
    {
        _associate = associate;
        _changeState = changeState;
    }

    /// <summary>Asocia los vehículos que puede operar un conductor.</summary>
    [HttpPost("vehiculos")]
    public async Task<ActionResult<IReadOnlyList<ConductorVehiculoOutputDto>>> Associate(
        [FromBody] AssociateVehiculosInputDto request,
        CancellationToken cancellationToken)
        => Ok(await _associate.ExecuteAsync(request, cancellationToken));

    /// <summary>Cambia el estado del conductor respecto del vehículo (PO, EA, RO).</summary>
    [HttpPut("estado")]
    public async Task<ActionResult<OperationResult>> ChangeState(
        [FromBody] ChangeConductorEstadoInputDto request,
        CancellationToken cancellationToken)
        => Ok(await _changeState.ExecuteAsync(request, cancellationToken));
}
