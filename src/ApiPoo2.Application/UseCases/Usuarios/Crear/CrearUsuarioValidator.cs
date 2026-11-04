using ApiPoo2.Domain.Common;
using FluentValidation;

namespace ApiPoo2.Application.UseCases.Usuarios.Crear;

public sealed class CrearUsuarioValidator : AbstractValidator<CrearUsuarioInputDto>
{
    public CrearUsuarioValidator()
    {
        RuleFor(x => x.PersonaId)
            .NotEmpty()
            .WithMessage("La persona es obligatoria.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("La contraseña es obligatoria.")
            .MinimumLength(PasswordPolicy.MinimumLength)
            .WithMessage($"La contraseña debe tener al menos {PasswordPolicy.MinimumLength} caracteres.")
            .MaximumLength(PasswordPolicy.MaximumLength)
            .WithMessage($"La contraseña no puede superar los {PasswordPolicy.MaximumLength} caracteres.");
    }
}
