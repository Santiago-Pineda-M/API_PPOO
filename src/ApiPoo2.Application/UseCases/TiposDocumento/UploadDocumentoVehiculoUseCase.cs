using ApiPoo2.Domain.TiposDocumento;
using ApiPoo2.Domain.Vehiculos;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.TiposDocumento;

/// <summary>
///     El enunciado pide cargue y/o actualización de uno o varios documentos a la vez, en Base64.
/// </summary>
public sealed record DocumentoVehiculoUploadItem(
    Guid TipoDocumentoId,
    string Base64,
    string NombreArchivo,
    DateTime FechaExpedicion,
    DateTime FechaVencimiento);

public sealed record UploadDocumentoVehiculoInputDto(Guid VehiculoId, IReadOnlyList<DocumentoVehiculoUploadItem> Documentos);

public sealed record DocumentoVehiculoUploadOutputDto(
    Guid VehiculoId,
    IReadOnlyList<DocumentoVehiculoCargadoOutputDto> Cargados);

public sealed record DocumentoVehiculoCargadoOutputDto(
    Guid TipoDocumentoId,
    string NombreArchivo,
    EstadoDocumento Estado);

public sealed class UploadDocumentoVehiculoUseCase : BaseUseCase<UploadDocumentoVehiculoInputDto, DocumentoVehiculoUploadOutputDto>
{
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly ITipoDocumentoRepository _documentoRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UploadDocumentoVehiculoUseCase(
        IEnumerable<IValidator<UploadDocumentoVehiculoInputDto>> validators,
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

    protected override async Task<DocumentoVehiculoUploadOutputDto> ExecuteCoreAsync(
        UploadDocumentoVehiculoInputDto request,
        CancellationToken cancellationToken)
    {
        if (request.Documentos.Count == 0)
        {
            throw new RequestValidationException(["Debe enviar al menos un documento."]);
        }

        var vehiculo = await _vehiculoRepository.GetByIdAsync(request.VehiculoId, cancellationToken)
            ?? throw new NotFoundException("vehicle.not_found", "El vehículo no existe.");

        var now = _dateTimeProvider.UtcNow;
        var cargados = new List<DocumentoVehiculoCargadoOutputDto>();

        foreach (var item in request.Documentos)
        {
            var documento = await _documentoRepository.GetByIdAsync(item.TipoDocumentoId, cancellationToken)
                ?? throw new NotFoundException("document.not_found", $"El documento {item.TipoDocumentoId} no existe.");

            var contenido = ContenidoDocumento.FromBase64(item.Base64, item.NombreArchivo);

            var relacion = vehiculo.GuardarDocumento(
                documento,
                contenido.ToArray(),
                item.NombreArchivo,
                item.FechaExpedicion,
                item.FechaVencimiento,
                now);

            cargados.Add(new DocumentoVehiculoCargadoOutputDto(
                relacion.TipoDocumentoId,
                relacion.NombreArchivo.Value,
                relacion.Estado));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DocumentoVehiculoUploadOutputDto(vehiculo.Id, cargados);
    }
}
