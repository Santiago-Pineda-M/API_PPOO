using ApiPoo2.Domain.Personas;

namespace ApiPoo2.Application.UseCases.Personas.Create;

public sealed record CreatePersonaInputDto(
    TipoIdentificacion TipoIdentificacion,
    string NumeroIdentificacion,
    string Nombres,
    string Apellidos,
    string CorreoElectronico,
    TipoPersona TipoPersona);

public sealed record CreatePersonaOutputDto(
    Guid Id,
    string NumeroIdentificacion,
    string Nombres,
    string Apellidos,
    string CorreoElectronico,
    TipoPersona TipoPersona,
    string? Login,
    string? ApiKey,
    string? PasswordGenerada)
{
    public static CreatePersonaOutputDto From(Persona persona, string? passwordGenerada) => new(
        persona.Id,
        persona.NumeroIdentificacion.Value,
        persona.Nombres.Value,
        persona.Apellidos.Value,
        persona.CorreoElectronico.Value,
        persona.TipoPersona,
        persona.Usuario?.Login.Value,
        persona.Usuario?.ApiKey.Value,
        passwordGenerada);
}