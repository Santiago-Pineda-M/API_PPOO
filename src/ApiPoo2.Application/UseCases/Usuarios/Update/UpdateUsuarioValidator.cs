using ApiPoo2.Domain.Personas;
using FluentValidation;

namespace ApiPoo2.Application.UseCases.Usuarios.Update;

public sealed class UpdateUsuarioValidator : AbstractValidator<UpdateUsuarioInputDto>
{
    public UpdateUsuarioValidator()
    {
        RuleFor(x => x.PersonaId)
            .NotEmpty()
            .WithMessage("La persona es obligatoria.");

        RuleFor(x => x.Nombres)
            .MaximumLength(100)
            .WithMessage("Los nombres no pueden superar los 100 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Nombres));

        RuleFor(x => x.Apellidos)
            .MaximumLength(100)
            .WithMessage("Los apellidos no pueden superar los 100 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Apellidos));

        RuleFor(x => x.CorreoElectronico)
            .EmailAddress()
            .WithMessage("El formato del correo electrónico no es válido.")
            .MaximumLength(320)
            .WithMessage("El correo electrónico no puede superar los 320 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.CorreoElectronico));
    }
}
