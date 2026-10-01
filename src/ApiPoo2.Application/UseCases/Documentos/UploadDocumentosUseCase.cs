using ApiPoo2.Domain.Documentos;
using ApiPoo2.Domain.Vehiculos;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Documentos;

/// <summary>
///     El enunciado pide cargue y/o actualización de uno o varios documentos a la vez, en Base64.
/// </summary>
public sealed record DocumentoUploadItem(
    Guid DocumentoId,
    string Base64,
    string NombreArchivo,
    DateTime FechaExpedicion,
    DateTime FechaVencimiento);

public sealed record UploadDocumentosInputDto(Guid VehiculoId, IReadOnlyList<DocumentoUploadItem> Documentos);

public sealed record DocumentoUploadOutputDto(
    Guid VehiculoId,
    IReadOnlyList<DocumentoCargadoOutputDto> Cargados);

public sealed record DocumentoCargadoOutputDto(
    Guid DocumentoId,
    string NombreArchivo,
    EstadoDocumento Estado);

public sealed class UploadDocumentosUseCase : BaseUseCase<UploadDocumentosInputDto, DocumentoUploadOutputDto>
{
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly IDocumentoRepository _documentoRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UploadDocumentosUseCase(
        IEnumerable<IValidator<UploadDocumentosInputDto>> validators,
        ILoggerFactory loggerFactory,
        IVehiculoRepository vehiculoRepository,
        IDocumentoRepository documentoRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _vehiculoRepository = vehiculoRepository;
        _documentoRepository = documentoRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<DocumentoUploadOutputDto> ExecuteCoreAsync(
        UploadDocumentosInputDto request,
        CancellationToken cancellationToken)
    {
        if (request.Documentos.Count == 0)
        {
            throw new RequestValidationException(["Debe enviar al menos un documento."]);
        }

        var vehiculo = await _vehiculoRepository.GetByIdAsync(request.VehiculoId, cancellationToken)
            ?? throw new NotFoundException("vehicle.not_found", "El vehículo no existe.");

        var now = _dateTimeProvider.UtcNow;
        var cargados = new List<DocumentoCargadoOutputDto>();

        foreach (var item in request.Documentos)
        {
            var documento = await _documentoRepository.GetByIdAsync(item.DocumentoId, cancellationToken)
                ?? throw new NotFoundException("document.not_found", $"El documento {item.DocumentoId} no existe.");

            var contenido = ContenidoDocumento.FromBase64(item.Base64, item.NombreArchivo);

            var relacion = vehiculo.GuardarDocumento(
                documento,
                contenido.ToArray(),
                item.NombreArchivo,
                item.FechaExpedicion,
                item.FechaVencimiento,
                now);

            cargados.Add(new DocumentoCargadoOutputDto(
                relacion.DocumentoId,
                relacion.NombreArchivo.Value,
                relacion.Estado));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DocumentoUploadOutputDto(vehiculo.Id, cargados);
    }
}
