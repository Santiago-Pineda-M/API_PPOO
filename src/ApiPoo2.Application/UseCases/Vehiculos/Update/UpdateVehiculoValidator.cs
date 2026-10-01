using FluentValidation;

namespace ApiPoo2.Application.UseCases.Vehiculos.Update;

public sealed class UpdateVehiculoValidator : AbstractValidator<UpdateVehiculoInputDto>
{
    public UpdateVehiculoValidator()
    {
        RuleFor(x => x.VehiculoId).NotEmpty().WithMessage("El vehículo es obligatorio.");

        RuleFor(x => x.Placa)
            .NotEmpty().WithMessage("La placa es obligatoria.")
            .Length(6).WithMessage("La placa debe tener exactamente 6 caracteres.");

        RuleFor(x => x.CapacidadPasajeros)
            .GreaterThanOrEqualTo(0).WithMessage("La capacidad de pasajeros no puede ser negativa.");

        RuleFor(x => x.Modelo)
            .GreaterThan(0).WithMessage("El modelo debe ser un número entero positivo.");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("El color es obligatorio.")
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("El color debe tener el formato #RRGGBB.");

        RuleFor(x => x.Marca).NotEmpty().WithMessage("La marca es obligatoria.");
        RuleFor(x => x.Linea).NotEmpty().WithMessage("La línea es obligatoria.");
    }
}
