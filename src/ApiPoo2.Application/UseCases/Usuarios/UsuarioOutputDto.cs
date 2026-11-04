using ApiPoo2.Application.Exceptions;
using ApiPoo2.Domain.Personas;

namespace ApiPoo2.Application.UseCases.Usuarios;

/// <summary>
///     Representación de un usuario del sistema junto con los datos de su persona asociada.
///     Se reutiliza como output de obtener, crear y editar usuario, y como ítem del listado.
/// </summary>
public sealed record UsuarioOutputDto(
    Guid Id,
    string NumeroIdentificacion,
    string Nombres,
    string Apellidos,
    string CorreoElectronico,
    TipoPersona TipoPersona,
    TipoIdentificacion TipoIdentificacion,
    string Login,
    string Rol,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc)
{
    public static UsuarioOutputDto From(Persona persona)
    {
        var usuario = persona.Usuario
            ?? throw new NotFoundException(
                "usuario.no_encontrado",
                "La persona no tiene un usuario asociado.");

        return new(
            persona.Id,
            persona.NumeroIdentificacion.Value,
            persona.Nombres.Value,
            persona.Apellidos.Value,
            persona.CorreoElectronico.Value,
            persona.TipoPersona,
            persona.TipoIdentificacion,
            usuario.Login.Value,
            usuario.Role.ToString(),
            usuario.IsActive,
            usuario.CreatedAtUtc,
            usuario.UpdatedAtUtc);
    }
}
