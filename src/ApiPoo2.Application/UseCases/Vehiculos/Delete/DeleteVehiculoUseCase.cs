using ApiPoo2.Application.UseCases.Auth;
using ApiPoo2.Application.UseCases.Vehiculos.Delete;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Vehiculos.Delete;

public sealed class DeleteVehiculoUseCase : BaseUseCase<DeleteVehiculoInputDto, OperationResult>
{
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteVehiculoUseCase(
        IEnumerable<IValidator<DeleteVehiculoInputDto>> validators,
        ILoggerFactory loggerFactory,
        IVehiculoRepository vehiculoRepository,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _vehiculoRepository = vehiculoRepository;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<OperationResult> ExecuteCoreAsync(
        DeleteVehiculoInputDto request,
        CancellationToken cancellationToken)
    {
        var vehiculo = await _vehiculoRepository.GetByIdAsync(request.VehiculoId, cancellationToken)
            ?? throw new NotFoundException("vehicle.not_found", "El vehículo no existe.");

        _vehiculoRepository.Remove(vehiculo);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }
}
