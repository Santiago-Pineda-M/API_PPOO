using FluentValidation;

namespace ApiPoo2.Application.UseCases.Personas.Update;

public sealed class UpdatePersonaValidator : AbstractValidator<UpdatePersonaInputDto>
{
    public UpdatePersonaValidator()
    {
        RuleFor(x => x.PersonaId).NotEmpty().WithMessage("La persona es obligatoria.");

        RuleFor(x => x.Nombres)
            .NotEmpty().WithMessage("Los nombres son obligatorios.")
            .MaximumLength(100).WithMessage("Los nombres no pueden superar los 100 caracteres.");

        RuleFor(x => x.Apellidos)
            .NotEmpty().WithMessage("Los apellidos son obligatorios.")
            .MaximumLength(100).WithMessage("Los apellidos no pueden superar los 100 caracteres.");

        RuleFor(x => x.CorreoElectronico)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.")
            .MaximumLength(320).WithMessage("El correo electrónico no puede superar los 320 caracteres.");
    }
}
