using ApiPoo2.Domain.Entities;
using ApiPoo2.Domain.Enums;

namespace ApiPoo2.Application.DTOs;

public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc)
{
    public static CurrentUserDto From(User user) => new(
        user.Id,
        user.Email.Value,
        user.Role,
        user.IsActive,
        user.CreatedAtUtc,
        user.LastLoginAtUtc);
}