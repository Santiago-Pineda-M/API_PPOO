using ApiPoo2.Application.UseCases.Personas;
using ApiPoo2.Application.UseCases.Personas.Create;
using ApiPoo2.Application.UseCases.Personas.Get;
using ApiPoo2.Application.UseCases.Personas.Licencia;
using ApiPoo2.Application.UseCases.Personas.Update;
using ApiPoo2.WebApi.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPoo2.WebApi.Controllers;

[ApiController]
[Route("api/personas")]
[Authorize(Policy = ApiKeyRequirement.PolicyName)]
public sealed class PersonasController : ControllerBase
{
    private readonly CreatePersonaUseCase _create;
    private readonly GetPersonaUseCase _get;
    private readonly UpdatePersonaUseCase _update;
    private readonly GetLicenciaConductorUseCase _getLicencia;
    private readonly RegistrarLicenciaConductorUseCase _registrarLicencia;
    private readonly EliminarLicenciaConductorUseCase _eliminarLicencia;

    public PersonasController(
        CreatePersonaUseCase create,
        GetPersonaUseCase get,
        UpdatePersonaUseCase update,
        GetLicenciaConductorUseCase getLicencia,
        RegistrarLicenciaConductorUseCase registrarLicencia,
        EliminarLicenciaConductorUseCase eliminarLicencia)
    {
        _create = create;
        _get = get;
        _update = update;
        _getLicencia = getLicencia;
        _registrarLicencia = registrarLicencia;
        _eliminarLicencia = eliminarLicencia;
    }

    [HttpPost]
    public async Task<ActionResult<CreatePersonaOutputDto>> Create(
        [FromBody] CreatePersonaInputDto request,
        CancellationToken cancellationToken)
        => StatusCode(
            StatusCodes.Status201Created,
            await _create.ExecuteAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetPersonaOutputDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => Ok(await _get.ExecuteAsync(new GetPersonaInputDto(id), cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UpdatePersonaOutputDto>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdatePersonaBody body,
        CancellationToken cancellationToken)
        => Ok(await _update.ExecuteAsync(
            new UpdatePersonaInputDto(id, body.Nombres, body.Apellidos, body.CorreoElectronico, body.TipoPersona),
            cancellationToken));

    /// <summary>Metadatos de la licencia de conducción; nunca devuelve el binario.</summary>
    [HttpGet("{id:guid}/licencia")]
    public async Task<ActionResult<GetLicenciaConductorOutputDto>> GetLicencia(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => Ok(await _getLicencia.ExecuteAsync(new GetLicenciaConductorInputDto(id), cancellationToken));

    /// <summary>Registra o reemplaza la licencia (UPSERT) como PDF en Base64.</summary>
    [HttpPut("{id:guid}/licencia")]
    public async Task<ActionResult<GetLicenciaConductorOutputDto>> RegistrarLicencia(
        [FromRoute] Guid id,
        [FromBody] RegistrarLicenciaBody body,
        CancellationToken cancellationToken)
        => Ok(await _registrarLicencia.ExecuteAsync(
            new RegistrarLicenciaConductorInputDto(id, body.ContenidoBase64, body.FechaVigencia),
            cancellationToken));

    [HttpDelete("{id:guid}/licencia")]
    public async Task<IActionResult> EliminarLicencia(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await _eliminarLicencia.ExecuteAsync(new EliminarLicenciaConductorInputDto(id), cancellationToken);
        return NoContent();
    }
}

public sealed record UpdatePersonaBody(
    string Nombres,
    string Apellidos,
    string CorreoElectronico,
    ApiPoo2.Domain.Personas.TipoPersona TipoPersona);

public sealed record RegistrarLicenciaBody(string ContenidoBase64, DateTime FechaVigencia);
