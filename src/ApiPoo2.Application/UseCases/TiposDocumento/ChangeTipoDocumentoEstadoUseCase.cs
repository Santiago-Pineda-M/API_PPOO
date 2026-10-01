using ApiPoo2.Application.UseCases.Auth;
using ApiPoo2.Domain.TiposDocumento;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.TiposDocumento;

public sealed record ChangeTipoDocumentoEstadoInputDto(Guid VehiculoId, Guid TipoDocumentoId, EstadoDocumento Estado);

public sealed class ChangeTipoDocumentoEstadoUseCase : BaseUseCase<ChangeTipoDocumentoEstadoInputDto, OperationResult>
{
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeTipoDocumentoEstadoUseCase(
        IEnumerable<IValidator<ChangeTipoDocumentoEstadoInputDto>> validators,
        ILoggerFactory loggerFactory,
        IVehiculoRepository vehiculoRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _vehiculoRepository = vehiculoRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<OperationResult> ExecuteCoreAsync(
        ChangeTipoDocumentoEstadoInputDto request,
        CancellationToken cancellationToken)
    {
        var vehiculo = await _vehiculoRepository.GetByIdAsync(request.VehiculoId, cancellationToken)
            ?? throw new NotFoundException("vehicle.not_found", "El vehículo no existe.");

        var relacion = vehiculo.GetTiposDocumento().FirstOrDefault(d => d.TipoDocumentoId == request.TipoDocumentoId)
            ?? throw new NotFoundException(
                "vehicle.document.not_found",
                "El vehículo no tiene asociado ese documento.");

        relacion.CambiarEstado(request.Estado, _dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }
}
