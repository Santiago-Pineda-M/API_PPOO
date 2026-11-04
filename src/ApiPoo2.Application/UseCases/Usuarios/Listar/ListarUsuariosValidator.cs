using FluentValidation;

namespace ApiPoo2.Application.UseCases.Usuarios.Listar;

public sealed class ListarUsuariosValidator : AbstractValidator<ListarUsuariosInputDto>
{
    public ListarUsuariosValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("La página debe ser mayor o igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("El tamaño de página debe estar entre 1 y 100.");
    }
}
