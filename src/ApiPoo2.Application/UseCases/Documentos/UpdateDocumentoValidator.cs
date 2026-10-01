using FluentValidation;

namespace ApiPoo2.Application.UseCases.Documentos;

public sealed class UpdateDocumentoValidator : AbstractValidator<UpdateDocumentoInputDto>
{
    public UpdateDocumentoValidator()
    {
        RuleFor(x => x.DocumentoId).NotEmpty().WithMessage("El documento es obligatorio.");
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre es obligatorio.");
        RuleFor(x => x.TiposVehiculoAplicables).NotEmpty().WithMessage("Los tipos aplicables son obligatorios.");
        RuleFor(x => x.CodigoObligatoriedad).NotEmpty().WithMessage("La obligatoriedad es obligatoria.");
        RuleFor(x => x.Descripcion).NotEmpty().WithMessage("La descripción es obligatoria.");
    }
}
