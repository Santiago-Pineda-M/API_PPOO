namespace ApiPoo2.Application.DTOs;

public sealed record ChangePasswordInputDto(
    Guid UserId,
    string CurrentPassword,
    string NewPassword,
    Guid AccessTokenJti,
    DateTime AccessTokenExpiresAtUtc);