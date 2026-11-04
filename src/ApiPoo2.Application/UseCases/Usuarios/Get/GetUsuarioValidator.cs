using FluentValidation;

namespace ApiPoo2.Application.UseCases.Usuarios.Get;

public sealed class GetUsuarioValidator : AbstractValidator<GetUsuarioInputDto>
{
    public GetUsuarioValidator()
    {
        RuleFor(x => x.PersonaId)
            .NotEmpty()
            .WithMessage("La persona es obligatoria.");
    }
}
