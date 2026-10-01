using ApiPoo2.Domain.TiposDocumento;
using ApiPoo2.Domain.Vehiculos;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Vehiculos;

/// <summary>
///     El enunciado exige registrar vehículo y documento en una sola operación, y que el documento
///     nazca En Verificación.
/// </summary>
public sealed record CreateVehiculoInputDto(
    string Placa,
    TipoVehiculo TipoVehiculo,
    TipoServicio TipoServicio,
    TipoCombustible TipoCombustible,
    int CapacidadPasajeros,
    string Color,
    int Modelo,
    string Marca,
    string Linea,
    Guid TipoDocumentoId,
    string DocumentoBase64,
    string NombreArchivo,
    DateTime FechaExpedicion,
    DateTime FechaVencimiento);

public sealed record CreateVehiculoOutputDto(
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
    Guid TipoDocumentoId,
    string NombreArchivo,
    DateTime FechaExpedicion,
    DateTime FechaVencimiento,
    EstadoDocumento Estado);

public sealed class CreateVehiculoUseCase : BaseUseCase<CreateVehiculoInputDto, CreateVehiculoOutputDto>
{
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly ITipoDocumentoRepository _documentoRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateVehiculoUseCase(
        IEnumerable<IValidator<CreateVehiculoInputDto>> validators,
        ILoggerFactory loggerFactory,
        IVehiculoRepository vehiculoRepository,
        ITipoDocumentoRepository documentoRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _vehiculoRepository = vehiculoRepository;
        _documentoRepository = documentoRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<CreateVehiculoOutputDto> ExecuteCoreAsync(
        CreateVehiculoInputDto request,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var placa = Placa.From(request.Placa, request.TipoVehiculo);

        if (await _vehiculoRepository.GetByPlacaAsync(placa, cancellationToken) is not null)
        {
            throw new ConflictException("vehiculo.placa.conflict", "Ya existe un vehículo con esa placa.");
        }

        var documento = await _documentoRepository.GetByIdAsync(request.TipoDocumentoId, cancellationToken)
            ?? throw new NotFoundException("document.not_found", "El tipo de documento no existe.");

        var contenido = ContenidoDocumento.FromBase64(request.DocumentoBase64, request.NombreArchivo);

        var vehiculo = Vehiculo.Register(
            request.Placa,
            request.TipoVehiculo,
            request.TipoServicio,
            request.TipoCombustible,
            request.CapacidadPasajeros,
            request.Color,
            request.Modelo,
            request.Marca,
            request.Linea,
            now);

        var relacion = vehiculo.AdjuntarDocumento(
            documento,
            contenido.ToArray(),
            request.NombreArchivo,
            request.FechaExpedicion,
            request.FechaVencimiento,
            now);

        _vehiculoRepository.Add(vehiculo);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateVehiculoOutputDto(
            vehiculo.Id,
            vehiculo.Placa.Value,
            vehiculo.TipoVehiculo,
            vehiculo.TipoServicio,
            vehiculo.Combustible,
            vehiculo.CapacidadPasajeros,
            vehiculo.Color.Value,
            vehiculo.Modelo,
            vehiculo.Marca.Value,
            vehiculo.Linea.Value,
            relacion.TipoDocumentoId,
            relacion.NombreArchivo.Value,
            relacion.FechaExpedicion,
            relacion.FechaVencimiento,
            relacion.Estado);
    }
}