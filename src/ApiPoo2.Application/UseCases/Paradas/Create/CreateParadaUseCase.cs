using FluentValidation;
using ApiPoo2.Application.UseCases.Rutas;
using ApiPoo2.Domain.Rutas;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Paradas.Create;

public sealed record CreateParadaInputDto(
    string CodigoRuta,
    string Ubicacion,
    int OrdenParada,
    double? Latitud,
    double? Longitud);

public sealed class CreateParadaValidator : AbstractValidator<CreateParadaInputDto>
{
    public CreateParadaValidator()
    {
        RuleFor(x => x.CodigoRuta)
            .NotEmpty().WithMessage("El código de ruta es obligatorio.");

        RuleFor(x => x.Ubicacion)
            .NotEmpty().WithMessage("La ubicación de la parada es obligatoria.")
            .MaximumLength(200).WithMessage("La ubicación no puede superar los 200 caracteres.");

        RuleFor(x => x.OrdenParada)
            .InclusiveBetween(0, 6).WithMessage("El orden de parada debe estar entre 0 y 6.");

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

public sealed class CreateParadaUseCase : BaseUseCase<CreateParadaInputDto, ParadaOutputDto>
{
    private readonly IRutaRepository _rutaRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateParadaUseCase(
        IEnumerable<IValidator<CreateParadaInputDto>> validators,
        ILoggerFactory loggerFactory,
        IRutaRepository rutaRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _rutaRepository = rutaRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<ParadaOutputDto> ExecuteCoreAsync(
        CreateParadaInputDto request,
        CancellationToken cancellationToken)
    {
        var ahora = _dateTimeProvider.UtcNow;
        var ruta = await _rutaRepository.GetByCodigoAsync(
            CodigoRuta.From(request.CodigoRuta),
            cancellationToken)
            ?? throw new NotFoundException("ruta.not_found", "La ruta no existe.");

        if (ruta.CantidadParadas() >= ReglasParada.MaximoParadasPorRuta)
        {
            throw new ConflictException(
                "parada.maximo",
                $"Una ruta admite como máximo {ReglasParada.MaximoParadasPorRuta} paradas.");
        }

        if (ruta.Paradas.Any(p => p.OrdenParada == request.OrdenParada))
        {
            throw new ConflictException(
                "parada.orden.duplicado",
                $"La ruta ya tiene una parada en el orden {request.OrdenParada}.");
        }

        var parada = ruta.AgregarParada(
            request.Ubicacion,
            request.OrdenParada,
            request.Latitud,
            request.Longitud,
            ahora);

        _rutaRepository.AddParada(parada);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ParadaOutputDto.From(parada);
    }
}
