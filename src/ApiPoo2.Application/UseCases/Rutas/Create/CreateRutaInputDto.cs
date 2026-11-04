using FluentValidation;

namespace ApiPoo2.Application.UseCases.Rutas.Create;

public sealed record CreateRutaInputDto(
    string CodigoRuta,
    Guid PersonaId,
    Guid VehiculoId,
    string UbicacionInicial,
    double? Latitud,
    double? Longitud,
    string? LoginUsuario = null);
// El login lo inyecta el controlador desde el token (`request with { LoginUsuario = ... }`);
// nunca se toma del cuerpo de la petición. Es nullable para que el binding no lo exija y el
// validador (que sí lo exige) sea el que rechace un token sin login.

public sealed class CreateRutaValidator : AbstractValidator<CreateRutaInputDto>
{
    public CreateRutaValidator()
    {
        RuleFor(x => x.CodigoRuta)
            .NotEmpty().WithMessage("El código de ruta es obligatorio.")
            .MaximumLength(32).WithMessage("El código de ruta no puede superar los 32 caracteres.");

        RuleFor(x => x.PersonaId)
            .NotEmpty().WithMessage("El conductor es obligatorio.");

        RuleFor(x => x.VehiculoId)
            .NotEmpty().WithMessage("El vehículo es obligatorio.");

        RuleFor(x => x.UbicacionInicial)
            .NotEmpty().WithMessage("La ubicación inicial es obligatoria.")
            .MaximumLength(200).WithMessage("La ubicación no puede superar los 200 caracteres.");

        RuleFor(x => x.LoginUsuario)
            .NotEmpty().WithMessage("No se pudo determinar el usuario que registra la ruta.")
            .MaximumLength(64).WithMessage("El login del usuario no puede superar los 64 caracteres.");

        RuleFor(x => x.Latitud)
            .InclusiveBetween(-90d, 90d).When(x => x.Latitud.HasValue)
            .WithMessage("La latitud debe estar entre -90 y 90.");

        RuleFor(x => x.Longitud)
            .InclusiveBetween(-180d, 180d).When(x => x.Longitud.HasValue)
            .WithMessage("La longitud debe estar entre -180 y 180.");

        RuleFor(x => x)
            .Must(x => x.Latitud.HasValue == x.Longitud.HasValue)
            .WithMessage("La latitud y la longitud deben informarse juntas o no informarse.");
    }
}
