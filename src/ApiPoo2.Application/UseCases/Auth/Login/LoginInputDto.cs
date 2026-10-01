using ApiPoo2.Domain.Personas;

namespace ApiPoo2.Application.UseCases.Auth;

public sealed record LoginInputDto(string Login, string Password);
