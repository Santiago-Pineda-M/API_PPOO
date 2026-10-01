using FluentValidation;

namespace ApiPoo2.Application.UseCases.Bootstrap;

public sealed class BootstrapAdminValidator : AbstractValidator<BootstrapAdminInputDto>
{
    public BootstrapAdminValidator()
    {
        RuleFor(x => x.NumeroIdentificacion)
            .NotEmpty().WithMessage("El número de identificación es obligatorio.")
            .Matches("^[0-9]+$").WithMessage("El número de identificación solo puede contener dígitos.")
            .MaximumLength(20).WithMessage("El número de identificación no puede superar los 20 caracteres.");

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
