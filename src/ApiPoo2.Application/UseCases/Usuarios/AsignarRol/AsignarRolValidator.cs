using ApiPoo2.Domain.Users;
using FluentValidation;

namespace ApiPoo2.Application.UseCases.Usuarios.AsignarRol;

public sealed class AsignarRolValidator : AbstractValidator<AsignarRolInputDto>
{
    public AsignarRolValidator()
    {
        RuleFor(x => x.PersonaId)
            .NotEmpty()
            .WithMessage("La persona es obligatoria.");

        RuleFor(x => x.Rol)
            .IsInEnum()
            .WithMessage("El rol no es válido.");
    }
}
