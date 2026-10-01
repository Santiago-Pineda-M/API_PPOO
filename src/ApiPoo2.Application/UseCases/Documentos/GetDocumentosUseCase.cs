using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Documentos;

public sealed record GetDocumentosInputDto;

public sealed class GetDocumentosUseCase
    : BaseUseCase<GetDocumentosInputDto, IReadOnlyList<DocumentoParametricoOutputDto>>
{
    private readonly IDocumentoRepository _documentoRepository;

    public GetDocumentosUseCase(
        IEnumerable<IValidator<GetDocumentosInputDto>> validators,
        ILoggerFactory loggerFactory,
        IDocumentoRepository documentoRepository)
        : base(validators, loggerFactory)
    {
        _documentoRepository = documentoRepository;
    }

    protected override async Task<IReadOnlyList<DocumentoParametricoOutputDto>> ExecuteCoreAsync(
        GetDocumentosInputDto request,
        CancellationToken cancellationToken)
        => (await _documentoRepository.GetAllAsync(cancellationToken))
            .Select(d => new DocumentoParametricoOutputDto(
                d.Id,
                d.Codigo.Value,
                d.Nombre.Value,
                d.TiposVehiculoAplicables.Value,
                d.CodigoObligatoriedad.Value,
                d.Descripcion.Value))
            .ToList();
}
