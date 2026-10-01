using ApiPoo2.Application.UseCases.Vehiculos.Consultas;
using ApiPoo2.Domain.TiposDocumento;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Vehiculos;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Vehiculos;

public sealed record GetVehiculoByPlacaInputDto(string Placa);

public sealed record VehiculoConsultaOutputDto(
    Guid Id,
    string Placa,
    TipoVehiculo TipoVehiculo,
    TipoServicio TipoServicio,
    TipoCombustible TipoCombustible,
    int CapacidadPasajeros,
    string Color,
    int Modelo,
    string Marca,
    string Linea,
    IReadOnlyList<DocumentoVehiculoConsultaOutputDto> Documentos,
    IReadOnlyList<VehiculoConductorOutputDto> Conductores);

public sealed record DocumentoVehiculoConsultaOutputDto(
    Guid TipoDocumentoId,
    string NombreArchivo,
    string ContentType,
    DateTime FechaExpedicion,
    DateTime FechaVencimiento,
    EstadoDocumento Estado,
    long Tamano);

public sealed record VehiculoConductorOutputDto(
    Guid PersonaId,
    string Nombres,
    string Apellidos,
    DateTime FechaAsociacion,
    EstadoConductor Estado);

/// <summary>
///     Consulta pública: vehículo por placa con la información de sus conductores y documentos.
///     El PDF no se devuelve completo, solo los metadatos y el tamaño.
/// </summary>
public sealed class GetVehiculoByPlacaUseCase : BaseUseCase<GetVehiculoByPlacaInputDto, VehiculoConsultaOutputDto>
{
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetVehiculoByPlacaUseCase(
        IEnumerable<IValidator<GetVehiculoByPlacaInputDto>> validators,
        ILoggerFactory loggerFactory,
        IVehiculoRepository vehiculoRepository,
        IDateTimeProvider dateTimeProvider)
        : base(validators, loggerFactory)
    {
        _vehiculoRepository = vehiculoRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<VehiculoConsultaOutputDto> ExecuteCoreAsync(
        GetVehiculoByPlacaInputDto request,
        CancellationToken cancellationToken)
    {
        var vehiculo = await _vehiculoRepository.GetByPlacaAsync(Placa.FromCualquiera(request.Placa), cancellationToken)
            ?? throw new NotFoundException("vehicle.not_found", "No existe un vehículo con esa placa.");

        return VehiculoConsultaMapper.FromVehiculo(vehiculo, _dateTimeProvider.UtcNow);
    }
}
