using ApiPoo2.Application.DTOs;
using FluentValidation;

namespace ApiPoo2.Application.UseCases.Auth.RefreshToken;

public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenInputDto>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("El token de refresco es obligatorio.");
    }
}