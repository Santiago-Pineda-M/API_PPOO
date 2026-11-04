using FluentValidation;

namespace ApiPoo2.Application.UseCases.Usuarios.Delete;

public sealed class DeleteUsuarioValidator : AbstractValidator<DeleteUsuarioInputDto>
{
    public DeleteUsuarioValidator()
    {
        RuleFor(x => x.PersonaId)
            .NotEmpty()
            .WithMessage("La persona es obligatoria.");
    }
}
