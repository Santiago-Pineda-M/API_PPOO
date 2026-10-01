namespace ApiPoo2.Application.UseCases.Bootstrap;

public sealed record BootstrapAdminOutputDto(
    Guid PersonaId,
    string Login,
    string PasswordGenerada,
    string ApiKey);
