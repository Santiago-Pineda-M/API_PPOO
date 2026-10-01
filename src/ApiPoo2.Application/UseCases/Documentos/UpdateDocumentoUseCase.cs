using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Documentos;

public sealed class UpdateDocumentoUseCase : BaseUseCase<UpdateDocumentoInputDto, DocumentoParametricoOutputDto>
{
    private readonly IDocumentoRepository _documentoRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDocumentoUseCase(
        IEnumerable<IValidator<UpdateDocumentoInputDto>> validators,
        ILoggerFactory loggerFactory,
        IDocumentoRepository documentoRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _documentoRepository = documentoRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<DocumentoParametricoOutputDto> ExecuteCoreAsync(
        UpdateDocumentoInputDto request,
        CancellationToken cancellationToken)
    {
        var documento = await _documentoRepository.GetByIdAsync(request.DocumentoId, cancellationToken)
            ?? throw new NotFoundException("document.not_found", "El documento no existe.");

        documento.Actualizar(
            request.Nombre,
            request.TiposVehiculoAplicables,
            request.CodigoObligatoriedad,
            request.Descripcion,
            _dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DocumentoParametricoOutputDto(
            documento.Id,
            documento.Codigo.Value,
            documento.Nombre.Value,
            documento.TiposVehiculoAplicables.Value,
            documento.CodigoObligatoriedad.Value,
            documento.Descripcion.Value);
    }
}
