using ApiPoo2.Application.IServices;
using ApiPoo2.Application.UseCases;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Mapas.BuscarDirecciones;

public sealed record BuscarDireccionesInputDto(string Consulta, int Limite = 5);

public sealed record BuscarDireccionesOutputDto(
    string Direccion,
    string? Nombre,
    string? Ciudad,
    string? Pais,
    double Latitud,
    double Longitud);

public sealed class BuscarDireccionesUseCase
    : BaseUseCase<BuscarDireccionesInputDto, IReadOnlyList<BuscarDireccionesOutputDto>>
{
    private readonly IGeoapifyService _geoapifyService;

    public BuscarDireccionesUseCase(
        IEnumerable<IValidator<BuscarDireccionesInputDto>> validators,
        ILoggerFactory loggerFactory,
        IGeoapifyService geoapifyService)
        : base(validators, loggerFactory)
    {
        _geoapifyService = geoapifyService;
    }

    protected override async Task<IReadOnlyList<BuscarDireccionesOutputDto>> ExecuteCoreAsync(
        BuscarDireccionesInputDto request,
        CancellationToken cancellationToken)
    {
        var addresses = await _geoapifyService.SearchAddressesAsync(
            request.Consulta,
            request.Limite,
            cancellationToken);

        return addresses
            .Select(address => new BuscarDireccionesOutputDto(
                address.Formatted,
                address.Name,
                address.City,
                address.Country,
                address.Latitude,
                address.Longitude))
            .ToList();
    }
}

public sealed class BuscarDireccionesValidator : AbstractValidator<BuscarDireccionesInputDto>
{
    public BuscarDireccionesValidator()
    {
        RuleFor(request => request.Consulta)
            .NotEmpty().WithMessage("La dirección de búsqueda es obligatoria.")
            .MaximumLength(200).WithMessage("La dirección no puede superar 200 caracteres.");

        RuleFor(request => request.Limite)
            .InclusiveBetween(1, 10).WithMessage("El límite debe estar entre 1 y 10.");
    }
}
