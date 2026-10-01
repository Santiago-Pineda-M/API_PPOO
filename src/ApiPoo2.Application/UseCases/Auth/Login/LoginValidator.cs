using FluentValidation;

namespace ApiPoo2.Application.UseCases.Auth;

public sealed class LoginValidator : AbstractValidator<LoginInputDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Login)
            .NotEmpty().WithMessage("El login es obligatorio.")
            .MaximumLength(64).WithMessage("El login no puede superar los 64 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.");
    }
}
