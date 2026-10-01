using ApiPoo2.Domain.Personas;

namespace ApiPoo2.Application.UseCases.Personas.Get;

public sealed record GetPersonaOutputDto(
    Guid Id,
    TipoIdentificacion TipoIdentificacion,
    string NumeroIdentificacion,
    string Nombres,
    string Apellidos,
    string CorreoElectronico,
    TipoPersona TipoPersona,
    string? Login,
    bool TieneUsuario)
{
    public static GetPersonaOutputDto From(Persona persona) => new(
        persona.Id,
        persona.TipoIdentificacion,
        persona.NumeroIdentificacion.Value,
        persona.Nombres.Value,
        persona.Apellidos.Value,
        persona.CorreoElectronico.Value,
        persona.TipoPersona,
        persona.Usuario?.Login.Value,
        persona.Usuario is not null);
}
