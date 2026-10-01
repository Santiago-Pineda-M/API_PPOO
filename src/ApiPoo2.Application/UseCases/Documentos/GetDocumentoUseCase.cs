using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Documentos;

public sealed class GetDocumentoUseCase : BaseUseCase<GetDocumentoInputDto, DocumentoParametricoOutputDto>
{
    private readonly IDocumentoRepository _documentoRepository;

    public GetDocumentoUseCase(
        IEnumerable<IValidator<GetDocumentoInputDto>> validators,
        ILoggerFactory loggerFactory,
        IDocumentoRepository documentoRepository)
        : base(validators, loggerFactory)
    {
        _documentoRepository = documentoRepository;
    }

    protected override async Task<DocumentoParametricoOutputDto> ExecuteCoreAsync(
        GetDocumentoInputDto request,
        CancellationToken cancellationToken)
    {
        var documento = await _documentoRepository.GetByIdAsync(request.DocumentoId, cancellationToken)
            ?? throw new NotFoundException("document.not_found", "El documento no existe.");

        return new DocumentoParametricoOutputDto(
            documento.Id,
            documento.Codigo.Value,
            documento.Nombre.Value,
            documento.TiposVehiculoAplicables.Value,
            documento.CodigoObligatoriedad.Value,
            documento.Descripcion.Value);
    }
}
