using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.TiposDocumento;

public sealed class UpdateTipoDocumentoUseCase : BaseUseCase<UpdateTipoDocumentoInputDto, TipoDocumentoOutputDto>
{
    private readonly ITipoDocumentoRepository _documentoRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTipoDocumentoUseCase(
        IEnumerable<IValidator<UpdateTipoDocumentoInputDto>> validators,
        ILoggerFactory loggerFactory,
        ITipoDocumentoRepository documentoRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _documentoRepository = documentoRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<TipoDocumentoOutputDto> ExecuteCoreAsync(
        UpdateTipoDocumentoInputDto request,
        CancellationToken cancellationToken)
    {
        var documento = await _documentoRepository.GetByIdAsync(request.TipoDocumentoId, cancellationToken)
            ?? throw new NotFoundException("document.not_found", "El documento no existe.");

        documento.Actualizar(
            request.Nombre,
            request.TiposVehiculoAplicables,
            request.CodigoObligatoriedad,
            request.Descripcion,
            _dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TipoDocumentoOutputDto(
            documento.Id,
            documento.Codigo.Value,
            documento.Nombre.Value,
            documento.TiposVehiculoAplicables.Value,
            documento.CodigoObligatoriedad.Value,
            documento.Descripcion.Value);
    }
}
