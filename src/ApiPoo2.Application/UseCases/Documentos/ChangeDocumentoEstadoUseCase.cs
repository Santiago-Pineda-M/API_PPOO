using ApiPoo2.Application.UseCases.Auth;
using ApiPoo2.Domain.Documentos;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Documentos;

public sealed record ChangeDocumentoEstadoInputDto(Guid VehiculoId, Guid DocumentoId, EstadoDocumento Estado);

public sealed class ChangeDocumentoEstadoUseCase : BaseUseCase<ChangeDocumentoEstadoInputDto, OperationResult>
{
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeDocumentoEstadoUseCase(
        IEnumerable<IValidator<ChangeDocumentoEstadoInputDto>> validators,
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
        ChangeDocumentoEstadoInputDto request,
        CancellationToken cancellationToken)
    {
        var vehiculo = await _vehiculoRepository.GetByIdAsync(request.VehiculoId, cancellationToken)
            ?? throw new NotFoundException("vehicle.not_found", "El vehículo no existe.");

        var relacion = vehiculo.GetDocumentos().FirstOrDefault(d => d.DocumentoId == request.DocumentoId)
            ?? throw new NotFoundException(
                "vehicle.document.not_found",
                "El vehículo no tiene asociado ese documento.");

        relacion.CambiarEstado(request.Estado, _dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }
}
