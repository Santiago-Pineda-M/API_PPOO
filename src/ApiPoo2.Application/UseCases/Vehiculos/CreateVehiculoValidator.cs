using FluentValidation;

namespace ApiPoo2.Application.UseCases.Vehiculos;

public sealed class CreateVehiculoValidator : AbstractValidator<CreateVehiculoInputDto>
{
    public CreateVehiculoValidator()
    {
        RuleFor(x => x.Placa)
            .NotEmpty().WithMessage("La placa es obligatoria.")
            .Length(6).WithMessage("La placa debe tener exactamente 6 caracteres.");

        RuleFor(x => x.TipoCombustible).NotNull().WithMessage("El tipo de combustible es obligatorio.");
        RuleFor(x => x.TipoServicio).NotNull().WithMessage("El tipo de servicio es obligatorio.");

        RuleFor(x => x.CapacidadPasajeros)
            .GreaterThanOrEqualTo(0).WithMessage("La capacidad de pasajeros no puede ser negativa.");

        RuleFor(x => x.Modelo)
            .GreaterThan(0).WithMessage("El modelo debe ser un número entero positivo.");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("El color es obligatorio.")
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("El color debe tener el formato #RRGGBB.");

        RuleFor(x => x.Marca).NotEmpty().WithMessage("La marca es obligatoria.");
        RuleFor(x => x.Linea).NotEmpty().WithMessage("La línea es obligatoria.");

        RuleFor(x => x.DocumentoId).NotEmpty().WithMessage("Debe indicar el tipo de documento asociado.");
        RuleFor(x => x.DocumentoBase64).NotEmpty().WithMessage("El contenido del documento es obligatorio.");
        RuleFor(x => x.NombreArchivo).NotEmpty().WithMessage("El nombre del archivo es obligatorio.");

        RuleFor(x => x.FechaExpedicion)
            .NotEqual(default(DateTime)).WithMessage("Debe indicar la fecha de expedición.");

        RuleFor(x => x.FechaVencimiento)
            .GreaterThan(x => x.FechaExpedicion)
            .WithMessage("La fecha de vencimiento debe ser posterior a la fecha de expedición.");
    }
}
