using ApiPoo2.Domain.Entities;

namespace ApiPoo2.Application.IServices;

public sealed record AccessTokenDescriptor(string Token, string TokenType, Guid Jti, DateTime ExpiresAtUtc);

public sealed record RefreshTokenDescriptor(string RawValue, string Hash, DateTime ExpiresAtUtc);

public interface IJwtTokenService
{
    AccessTokenDescriptor CreateAccessToken(User user, DateTime utcNow);

    RefreshTokenDescriptor CreateRefreshToken(DateTime utcNow);

    string HashRefreshToken(string rawValue);
}