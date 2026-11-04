using ApiPoo2.Application.UseCases.Paradas.Create;
using ApiPoo2.Application.UseCases.Paradas.List;
using ApiPoo2.Application.UseCases.Rutas;
using ApiPoo2.Application.UseCases.Rutas.Create;
using ApiPoo2.Application.UseCases.Rutas.Delete;
using ApiPoo2.Application.UseCases.Rutas.Get;
using ApiPoo2.Application.UseCases.Rutas.List;
using ApiPoo2.Application.UseCases.Rutas.PorConductor;
using ApiPoo2.Application.UseCases.Rutas.PorPlaca;
using ApiPoo2.Application.UseCases.Rutas.Update;
using ApiPoo2.WebApi.Auth;
using ApiPoo2.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPoo2.WebApi.Controllers;

/// <summary>
///     Las consultas del enunciado de E3 exigen solo token; las escrituras mantienen la exigencia
///     de token + APIKey de E1/E2.
/// </summary>
[ApiController]
[Route("api/rutas")]
[Authorize]
public sealed class RutasController : ControllerBase
{
    private readonly CreateRutaUseCase _create;
    private readonly ListRutasUseCase _list;
    private readonly GetRutaUseCase _get;
    private readonly UpdateRutaUseCase _update;
    private readonly DeleteRutaUseCase _delete;
    private readonly ListParadasUseCase _listParadas;
    private readonly CreateParadaUseCase _createParada;
    private readonly ListRutasPorConductorUseCase _porConductor;
    private readonly ListRutasPorPlacaUseCase _porPlaca;

    public RutasController(
        CreateRutaUseCase create,
        ListRutasUseCase list,
        GetRutaUseCase get,
        UpdateRutaUseCase update,
        DeleteRutaUseCase delete,
        ListParadasUseCase listParadas,
        CreateParadaUseCase createParada,
        ListRutasPorConductorUseCase porConductor,
        ListRutasPorPlacaUseCase porPlaca)
    {
        _create = create;
        _list = list;
        _get = get;
        _update = update;
        _delete = delete;
        _listParadas = listParadas;
        _createParada = createParada;
        _porConductor = porConductor;
        _porPlaca = porPlaca;
    }

    [Authorize(Policy = ApiKeyRequirement.PolicyName)]
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

    /// <summary>Solo códigos de ruta del conductor, agrupados y ordenados.</summary>
    [HttpGet("conductor/{numeroIdentificacion}")]
    public async Task<ActionResult<IReadOnlyList<string>>> PorConductor(
        [FromRoute] string numeroIdentificacion,
        CancellationToken cancellationToken)
        => Ok(await _porConductor.ExecuteAsync(
            new ListRutasPorConductorInputDto(numeroIdentificacion),
            cancellationToken));

    /// <summary>Código de ruta y conductor del vehículo, agrupados y ordenados.</summary>
    [HttpGet("placa/{placa}")]
    public async Task<ActionResult<IReadOnlyList<RutaConductorOutputDto>>> PorPlaca(
        [FromRoute] string placa,
        CancellationToken cancellationToken)
        => Ok(await _porPlaca.ExecuteAsync(
            new ListRutasPorPlacaInputDto(placa),
            cancellationToken));

    [HttpGet("{codigoRuta}")]
    public async Task<ActionResult<RutaConsultaOutputDto>> GetByCodigo(
        [FromRoute] string codigoRuta,
        CancellationToken cancellationToken)
        => Ok(await _get.ExecuteAsync(new GetRutaInputDto(codigoRuta), cancellationToken));

    [Authorize(Policy = ApiKeyRequirement.PolicyName)]
    [HttpPut("{codigoRuta}")]
    public async Task<ActionResult<RutaConsultaOutputDto>> Update(
        [FromRoute] string codigoRuta,
        [FromBody] UpdateRutaBody body,
        CancellationToken cancellationToken)
        => Ok(await _update.ExecuteAsync(
            new UpdateRutaInputDto(codigoRuta, body.PersonaId, body.VehiculoId),
            cancellationToken));

    [Authorize(Policy = ApiKeyRequirement.PolicyName)]
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

    [Authorize(Policy = ApiKeyRequirement.PolicyName)]
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
