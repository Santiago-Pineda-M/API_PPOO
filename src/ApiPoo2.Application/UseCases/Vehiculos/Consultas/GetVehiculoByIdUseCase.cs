using ApiPoo2.Domain.Vehiculos;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Vehiculos.Consultas;

public sealed record GetVehiculoByIdInputDto(Guid VehiculoId);

public sealed class GetVehiculoByIdUseCase : BaseUseCase<GetVehiculoByIdInputDto, VehiculoConsultaOutputDto>
{
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetVehiculoByIdUseCase(
        IEnumerable<IValidator<GetVehiculoByIdInputDto>> validators,
        ILoggerFactory loggerFactory,
        IVehiculoRepository vehiculoRepository,
        IDateTimeProvider dateTimeProvider)
        : base(validators, loggerFactory)
    {
        _vehiculoRepository = vehiculoRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<VehiculoConsultaOutputDto> ExecuteCoreAsync(
        GetVehiculoByIdInputDto request,
        CancellationToken cancellationToken)
    {
        var vehiculo = await _vehiculoRepository.GetByIdAsync(request.VehiculoId, cancellationToken)
            ?? throw new NotFoundException("vehicle.not_found", "El vehículo no existe.");

        return VehiculoConsultaMapper.FromVehiculo(vehiculo, _dateTimeProvider.UtcNow);
    }
}
