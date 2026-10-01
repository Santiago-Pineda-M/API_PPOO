using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.TiposDocumento;

public sealed class GetTipoDocumentoUseCase : BaseUseCase<GetTipoDocumentoInputDto, TipoDocumentoOutputDto>
{
    private readonly ITipoDocumentoRepository _documentoRepository;

    public GetTipoDocumentoUseCase(
        IEnumerable<IValidator<GetTipoDocumentoInputDto>> validators,
        ILoggerFactory loggerFactory,
        ITipoDocumentoRepository documentoRepository)
        : base(validators, loggerFactory)
    {
        _documentoRepository = documentoRepository;
    }

    protected override async Task<TipoDocumentoOutputDto> ExecuteCoreAsync(
        GetTipoDocumentoInputDto request,
        CancellationToken cancellationToken)
    {
        var documento = await _documentoRepository.GetByIdAsync(request.TipoDocumentoId, cancellationToken)
            ?? throw new NotFoundException("document.not_found", "El documento no existe.");

        return new TipoDocumentoOutputDto(
            documento.Id,
            documento.Codigo.Value,
            documento.Nombre.Value,
            documento.TiposVehiculoAplicables.Value,
            documento.CodigoObligatoriedad.Value,
            documento.Descripcion.Value);
    }
}
