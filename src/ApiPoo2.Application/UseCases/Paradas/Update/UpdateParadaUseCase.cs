using ApiPoo2.Application.UseCases.Rutas;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Paradas.Update;

public sealed record UpdateParadaInputDto(
    Guid ParadaId,
    string Ubicacion,
    int OrdenParada,
    double? Latitud,
    double? Longitud);

public sealed class UpdateParadaValidator : AbstractValidator<UpdateParadaInputDto>
{
    public UpdateParadaValidator()
    {
        RuleFor(x => x.ParadaId)
            .NotEmpty().WithMessage("La parada es obligatoria.");

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

public sealed class UpdateParadaUseCase : BaseUseCase<UpdateParadaInputDto, ParadaOutputDto>
{
    private readonly IRutaRepository _rutaRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateParadaUseCase(
        IEnumerable<IValidator<UpdateParadaInputDto>> validators,
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
        UpdateParadaInputDto request,
        CancellationToken cancellationToken)
    {
        var ahora = _dateTimeProvider.UtcNow;
        var ruta = await _rutaRepository.GetByParadaAsync(request.ParadaId, cancellationToken)
            ?? throw new NotFoundException("parada.not_found", "La parada no existe.");

        if (ruta.Paradas.Any(p => p.Id != request.ParadaId && p.OrdenParada == request.OrdenParada))
        {
            throw new ConflictException(
                "parada.orden.duplicado",
                $"La ruta ya tiene una parada en el orden {request.OrdenParada}.");
        }

        ruta.ActualizarParada(
            request.ParadaId,
            request.Ubicacion,
            request.OrdenParada,
            request.Latitud,
            request.Longitud,
            ahora);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var parada = ruta.BuscarParada(request.ParadaId)!;
        return ParadaOutputDto.From(parada);
    }
}
