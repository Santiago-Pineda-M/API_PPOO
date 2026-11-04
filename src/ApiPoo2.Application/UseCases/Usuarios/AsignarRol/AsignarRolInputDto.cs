using ApiPoo2.Domain.Users;

namespace ApiPoo2.Application.UseCases.Usuarios.AsignarRol;

public sealed record AsignarRolInputDto(Guid PersonaId, UserRole Rol);
