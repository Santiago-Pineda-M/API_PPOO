using ApiPoo2.Domain.Entities;
using ApiPoo2.Domain.Enums;

namespace ApiPoo2.Application.Models;

public sealed record UserDto(
    Guid Id,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc)
{
    public static UserDto From(User user) => new(
        user.Id,
        user.Email.Value,
        user.Role,
        user.IsActive,
        user.CreatedAtUtc,
        user.LastLoginAtUtc);
}