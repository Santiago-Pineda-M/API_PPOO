using ApiPoo2.Application.DTOs;
using FluentValidation;

namespace ApiPoo2.Application.UseCases.Auth.ChangePassword;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordInputDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("La contraseña actual es obligatoria.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("La nueva contraseña es obligatoria.");
    }
}