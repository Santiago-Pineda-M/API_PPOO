using ApiPoo2.Application.IServices;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.RefreshTokens;
namespace ApiPoo2.Application.UseCases.Auth;

public sealed record RefreshTokenInputDto(string RefreshToken);
