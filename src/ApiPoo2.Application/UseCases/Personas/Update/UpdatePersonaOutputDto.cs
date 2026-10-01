using ApiPoo2.Application.UseCases.Personas.Get;
using ApiPoo2.Domain.Personas;

namespace ApiPoo2.Application.UseCases.Personas.Update;

public sealed record UpdatePersonaOutputDto(
    Guid Id,
    string Nombres,
    string Apellidos,
    string CorreoElectronico,
    TipoPersona TipoPersona,
    DateTime? UpdatedAtUtc)
{
    public static UpdatePersonaOutputDto From(Persona persona) => new(
        persona.Id,
        persona.Nombres.Value,
        persona.Apellidos.Value,
        persona.CorreoElectronico.Value,
        persona.TipoPersona,
        persona.UpdatedAtUtc);
}
