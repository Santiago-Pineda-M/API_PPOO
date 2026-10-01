using ApiPoo2.Domain.Personas;

namespace ApiPoo2.Application.UseCases.Personas.Update;

public sealed record UpdatePersonaInputDto(
    Guid PersonaId,
    string Nombres,
    string Apellidos,
    string CorreoElectronico,
    TipoPersona TipoPersona);
