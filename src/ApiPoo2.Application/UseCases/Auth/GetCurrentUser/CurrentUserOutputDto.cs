using ApiPoo2.Domain.Personas;

namespace ApiPoo2.Application.UseCases.Auth;

public sealed record CurrentUserOutputDto(
    Guid PersonaId,
    string Login,
    string Nombres,
    string Apellidos,
    string NumeroIdentificacion,
    TipoPersona TipoPersona,
    string Role,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc)
{
    public static CurrentUserOutputDto From(Persona persona)
    {
        var user = persona.Usuario
            ?? throw new NotFoundException("user.not_found", "La persona no tiene un usuario asociado.");

        return new CurrentUserOutputDto(
            persona.Id,
            user.Login.Value,
            persona.Nombres.Value,
            persona.Apellidos.Value,
            persona.NumeroIdentificacion.Value,
            persona.TipoPersona,
            user.Role.ToString(),
            user.IsActive,
            user.CreatedAtUtc,
            user.LastLoginAtUtc);
    }
}
