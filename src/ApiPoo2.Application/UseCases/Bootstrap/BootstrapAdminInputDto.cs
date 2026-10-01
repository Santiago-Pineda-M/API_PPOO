using ApiPoo2.Domain.Personas;

namespace ApiPoo2.Application.UseCases.Bootstrap;

public sealed record BootstrapAdminInputDto(
    TipoIdentificacion TipoIdentificacion,
    string NumeroIdentificacion,
    string Nombres,
    string Apellidos,
    string CorreoElectronico);
