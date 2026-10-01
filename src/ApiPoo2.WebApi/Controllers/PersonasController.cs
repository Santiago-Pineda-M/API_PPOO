using ApiPoo2.Application.UseCases.Personas;
using ApiPoo2.Application.UseCases.Personas.Create;
using ApiPoo2.Application.UseCases.Personas.Get;
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

    public PersonasController(
        CreatePersonaUseCase create,
        GetPersonaUseCase get,
        UpdatePersonaUseCase update)
    {
        _create = create;
        _get = get;
        _update = update;
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
}

public sealed record UpdatePersonaBody(
    string Nombres,
    string Apellidos,
    string CorreoElectronico,
    ApiPoo2.Domain.Personas.TipoPersona TipoPersona);
