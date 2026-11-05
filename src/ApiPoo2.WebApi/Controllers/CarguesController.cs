using ApiPoo2.Application.UseCases.Cargues;
using ApiPoo2.Domain.Trayectos;
using ApiPoo2.WebApi.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPoo2.WebApi.Controllers;

[ApiController]
[Route("api/cargues")]
[Authorize(Policy = ApiKeyRequirement.PolicyName)]
public sealed class CarguesController : ControllerBase
{
    private readonly CargarTrayectosExcelUseCase _cargar;
    private readonly ListarCarguesUseCase _listar;
    private readonly GetCargueUseCase _get;
    private readonly EliminarCargueUseCase _eliminar;

    public CarguesController(
        CargarTrayectosExcelUseCase cargar,
        ListarCarguesUseCase listar,
        GetCargueUseCase get,
        EliminarCargueUseCase eliminar)
    {
        _cargar = cargar;
        _listar = listar;
        _get = get;
        _eliminar = eliminar;
    }

    /// <summary>Carga masiva de trayectos desde un archivo .xlsx (multipart/form-data).</summary>
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<CargarTrayectosExcelOutputDto>> Cargar(
        IFormFile? archivo,
        CancellationToken cancellationToken)
    {
        if (archivo is null)
        {
            return BadRequest(new { code = "cargue.archivo.vacio", message = "El archivo es obligatorio." });
        }

        await using var stream = archivo.OpenReadStream();

        var resultado = await _cargar.ExecuteAsync(
            new CargarTrayectosExcelInputDto(stream, archivo.FileName, archivo.Length),
            cancellationToken);

        return Created($"/api/cargues/{resultado.IdCargue}", resultado);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CargueResumenOutputDto>>> Listar(
        [FromQuery] EstadoTrayectoTemporal? estado,
        CancellationToken cancellationToken)
        => Ok(await _listar.ExecuteAsync(new ListarCarguesInputDto(estado), cancellationToken));

    [HttpGet("{idCargue:long}")]
    public async Task<ActionResult<GetCargueOutputDto>> GetById(
        [FromRoute] long idCargue,
        [FromQuery] EstadoTrayectoTemporal? estado,
        CancellationToken cancellationToken)
        => Ok(await _get.ExecuteAsync(new GetCargueInputDto(idCargue, estado), cancellationToken));

    [HttpDelete("{idCargue:long}")]
    public async Task<IActionResult> Eliminar(
        [FromRoute] long idCargue,
        CancellationToken cancellationToken)
    {
        await _eliminar.ExecuteAsync(new EliminarCargueInputDto(idCargue), cancellationToken);
        return NoContent();
    }
}
