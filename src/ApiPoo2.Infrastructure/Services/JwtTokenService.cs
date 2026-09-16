using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ApiPoo2.Application.IServices;
using ApiPoo2.Domain.Entities;
using ApiPoo2.Infrastructure.Options;
using Microsoft.IdentityModel.Tokens;

namespace ApiPoo2.Infrastructure.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(JwtOptions options)
    {
        _options = options;
    }

    public AccessTokenDescriptor CreateAccessToken(User user, DateTime utcNow)
    {
        var jti = Guid.NewGuid();
        var expiresAtUtc = utcNow.AddMinutes(_options.AccessTokenTtlMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email.Value),
            new Claim(JwtRegisteredClaimNames.Jti, jti.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: utcNow,
            expires: expiresAtUtc,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
                SecurityAlgorithms.HmacSha256));

        var raw = new JwtSecurityTokenHandler().WriteToken(token);

        return new AccessTokenDescriptor(raw, "Bearer", jti, expiresAtUtc);
    }

    public RefreshTokenDescriptor CreateRefreshToken(DateTime utcNow)
    {
        var rawValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return new RefreshTokenDescriptor(rawValue, HashRefreshToken(rawValue), utcNow.AddDays(_options.RefreshTokenTtlDays));
    }

    public string HashRefreshToken(string rawValue) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawValue)));
}