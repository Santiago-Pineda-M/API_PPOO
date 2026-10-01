namespace ApiPoo2.Application.UseCases.Auth;

public sealed record ChangePasswordInputDto(
    Guid PersonaId,
    string CurrentPassword,
    string NewPassword,
    Guid AccessTokenJti,
    DateTime AccessTokenExpiresAtUtc);
