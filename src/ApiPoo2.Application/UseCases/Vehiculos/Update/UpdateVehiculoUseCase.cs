using ApiPoo2.Application.UseCases.Vehiculos.Update;
using ApiPoo2.Domain.Vehiculos;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Vehiculos.Update;

public sealed class UpdateVehiculoUseCase : BaseUseCase<UpdateVehiculoInputDto, UpdateVehiculoOutputDto>
{
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateVehiculoUseCase(
        IEnumerable<IValidator<UpdateVehiculoInputDto>> validators,
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

    protected override async Task<UpdateVehiculoOutputDto> ExecuteCoreAsync(
        UpdateVehiculoInputDto request,
        CancellationToken cancellationToken)
    {
        var vehiculo = await _vehiculoRepository.GetByIdAsync(request.VehiculoId, cancellationToken)
            ?? throw new NotFoundException("vehicle.not_found", "El vehículo no existe.");

        var placa = Placa.From(request.Placa, request.TipoVehiculo);
        var existente = await _vehiculoRepository.GetByPlacaAsync(placa, cancellationToken);
        if (existente is not null && existente.Id != vehiculo.Id)
        {
            throw new ConflictException("vehiculo.placa.conflict", "Ya existe otro vehículo con esa placa.");
        }

        vehiculo.Actualizar(
            placa.Value,
            request.TipoVehiculo,
            request.TipoServicio,
            request.TipoCombustible,
            request.CapacidadPasajeros,
            request.Color,
            request.Modelo,
            request.Marca,
            request.Linea,
            _dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UpdateVehiculoOutputDto.From(vehiculo);
    }
}
