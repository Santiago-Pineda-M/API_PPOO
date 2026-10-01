using FluentValidation;

namespace ApiPoo2.Application.UseCases.TiposDocumento;

public sealed class UpdateTipoDocumentoValidator : AbstractValidator<UpdateTipoDocumentoInputDto>
{
    public UpdateTipoDocumentoValidator()
    {
        RuleFor(x => x.TipoDocumentoId).NotEmpty().WithMessage("El documento es obligatorio.");
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre es obligatorio.");
        RuleFor(x => x.TiposVehiculoAplicables).NotEmpty().WithMessage("Los tipos aplicables son obligatorios.");
        RuleFor(x => x.CodigoObligatoriedad).NotEmpty().WithMessage("La obligatoriedad es obligatoria.");
        RuleFor(x => x.Descripcion).NotEmpty().WithMessage("La descripción es obligatoria.");
    }
}
