using ApiPoo2.Domain.RefreshTokens;
using ApiPoo2.Application.UseCases.Auth;
using FluentValidation;

namespace ApiPoo2.Application.UseCases.Auth;

public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenInputDto>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("El token de refresco es obligatorio.");
    }
}
