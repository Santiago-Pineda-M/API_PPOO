using ApiPoo2.Domain.Personas;

namespace ApiPoo2.Application.UseCases.Usuarios.Update;

public sealed record UpdateUsuarioInputDto(
    Guid PersonaId,
    string? Nombres,
    string? Apellidos,
    string? CorreoElectronico,
    TipoPersona? TipoPersona,
    bool? IsActive);
