using ApiPoo2.Application.UseCases.Paradas.Create;
using ApiPoo2.Application.UseCases.Paradas.List;
using ApiPoo2.Application.UseCases.Rutas;
using ApiPoo2.Application.UseCases.Rutas.Create;
using ApiPoo2.Application.UseCases.Rutas.Delete;
using ApiPoo2.Application.UseCases.Rutas.Get;
using ApiPoo2.Application.UseCases.Rutas.List;
using ApiPoo2.Application.UseCases.Rutas.Update;
using ApiPoo2.WebApi.Auth;
using ApiPoo2.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPoo2.WebApi.Controllers;

[ApiController]
[Route("api/rutas")]
[Authorize(Policy = ApiKeyRequirement.PolicyName)]
public sealed class RutasController : ControllerBase
{
    private readonly CreateRutaUseCase _create;
    private readonly ListRutasUseCase _list;
    private readonly GetRutaUseCase _get;
    private readonly UpdateRutaUseCase _update;
    private readonly DeleteRutaUseCase _delete;
    private readonly ListParadasUseCase _listParadas;
    private readonly CreateParadaUseCase _createParada;

    public RutasController(
        CreateRutaUseCase create,
        ListRutasUseCase list,
        GetRutaUseCase get,
        UpdateRutaUseCase update,
        DeleteRutaUseCase delete,
        ListParadasUseCase listParadas,
        CreateParadaUseCase createParada)
    {
        _create = create;
        _list = list;
        _get = get;
        _update = update;
        _delete = delete;
        _listParadas = listParadas;
        _createParada = createParada;
    }

    [HttpPost]
    public async Task<ActionResult<RutaConsultaOutputDto>> Create(
        [FromBody] CreateRutaInputDto request,
        CancellationToken cancellationToken)
    {
        var creada = await _create.ExecuteAsync(
            request with { LoginUsuario = User.GetLogin() },
            cancellationToken);

        return CreatedAtAction(nameof(GetByCodigo), new { codigoRuta = creada.CodigoRuta }, creada);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RutaResumenOutputDto>>> List(
        [FromQuery] string? codigo,
        [FromQuery] string? placa,
        [FromQuery] string? numeroIdentificacion,
        [FromQuery] bool? operable,
        CancellationToken cancellationToken)
        => Ok(await _list.ExecuteAsync(
            new ListRutasInputDto(codigo, placa, numeroIdentificacion, operable),
            cancellationToken));

    [HttpGet("{codigoRuta}")]
    public async Task<ActionResult<RutaConsultaOutputDto>> GetByCodigo(
        [FromRoute] string codigoRuta,
        CancellationToken cancellationToken)
        => Ok(await _get.ExecuteAsync(new GetRutaInputDto(codigoRuta), cancellationToken));

    [HttpPut("{codigoRuta}")]
    public async Task<ActionResult<RutaConsultaOutputDto>> Update(
        [FromRoute] string codigoRuta,
        [FromBody] UpdateRutaBody body,
        CancellationToken cancellationToken)
        => Ok(await _update.ExecuteAsync(
            new UpdateRutaInputDto(codigoRuta, body.PersonaId, body.VehiculoId),
            cancellationToken));

    [HttpDelete("{codigoRuta}")]
    public async Task<IActionResult> Delete(
        [FromRoute] string codigoRuta,
        CancellationToken cancellationToken)
    {
        await _delete.ExecuteAsync(new DeleteRutaInputDto(codigoRuta), cancellationToken);
        return NoContent();
    }

    [HttpGet("{codigoRuta}/paradas")]
    public async Task<ActionResult<IReadOnlyList<ParadaOutputDto>>> ListParadas(
        [FromRoute] string codigoRuta,
        CancellationToken cancellationToken)
        => Ok(await _listParadas.ExecuteAsync(new ListParadasInputDto(codigoRuta), cancellationToken));

    [HttpPost("{codigoRuta}/paradas")]
    public async Task<ActionResult<ParadaOutputDto>> CreateParada(
        [FromRoute] string codigoRuta,
        [FromBody] CreateParadaBody body,
        CancellationToken cancellationToken)
    {
        var creada = await _createParada.ExecuteAsync(
            new CreateParadaInputDto(
                codigoRuta,
                body.Ubicacion,
                body.OrdenParada,
                body.Latitud,
                body.Longitud),
            cancellationToken);

        return Created($"/api/paradas/{creada.Id}", creada);
    }
}

public sealed record UpdateRutaBody(Guid PersonaId, Guid VehiculoId);

public sealed record CreateParadaBody(
    string Ubicacion,
    int OrdenParada,
    double? Latitud,
    double? Longitud);
