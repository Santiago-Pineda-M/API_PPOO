using ApiPoo2.Application.UseCases.Mapas.BuscarDirecciones;
using ApiPoo2.Application.UseCases.Mapas.CalcularRuta;
using ApiPoo2.WebApi.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPoo2.WebApi.Controllers;

/// <summary>Operaciones de geocodificación y cálculo de rutas mediante Geoapify.</summary>
[ApiController]
[Route("api/mapas")]
[Authorize(Policy = ApiKeyRequirement.PolicyName)]
public sealed class MapasController : ControllerBase
{
    private readonly BuscarDireccionesUseCase _buscarDirecciones;
    private readonly CalcularRutaUseCase _calcularRuta;

    public MapasController(
        BuscarDireccionesUseCase buscarDirecciones,
        CalcularRutaUseCase calcularRuta)
    {
        _buscarDirecciones = buscarDirecciones;
        _calcularRuta = calcularRuta;
    }

    /// <summary>Busca una dirección y devuelve sus coordenadas geográficas.</summary>
    [HttpPost("direcciones")]
    public async Task<ActionResult<IReadOnlyList<BuscarDireccionesOutputDto>>> BuscarDirecciones(
        [FromBody] BuscarDireccionesInputDto request,
        CancellationToken cancellationToken)
        => Ok(await _buscarDirecciones.ExecuteAsync(request, cancellationToken));

    /// <summary>Calcula una ruta y devuelve distancia, duración y geometría para dibujarla en un mapa.</summary>
    [HttpPost("rutas")]
    public async Task<ActionResult<CalcularRutaOutputDto>> CalcularRuta(
        [FromBody] CalcularRutaInputDto request,
        CancellationToken cancellationToken)
        => Ok(await _calcularRuta.ExecuteAsync(request, cancellationToken));
}
