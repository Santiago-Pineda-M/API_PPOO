using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.TiposDocumento;

public sealed record GetTiposDocumentoInputDto;

public sealed class GetTiposDocumentoUseCase
    : BaseUseCase<GetTiposDocumentoInputDto, IReadOnlyList<TipoDocumentoOutputDto>>
{
    private readonly ITipoDocumentoRepository _documentoRepository;

    public GetTiposDocumentoUseCase(
        IEnumerable<IValidator<GetTiposDocumentoInputDto>> validators,
        ILoggerFactory loggerFactory,
        ITipoDocumentoRepository documentoRepository)
        : base(validators, loggerFactory)
    {
        _documentoRepository = documentoRepository;
    }

    protected override async Task<IReadOnlyList<TipoDocumentoOutputDto>> ExecuteCoreAsync(
        GetTiposDocumentoInputDto request,
        CancellationToken cancellationToken)
        => (await _documentoRepository.GetAllAsync(cancellationToken))
            .Select(d => new TipoDocumentoOutputDto(
                d.Id,
                d.Codigo.Value,
                d.Nombre.Value,
                d.TiposVehiculoAplicables.Value,
                d.CodigoObligatoriedad.Value,
                d.Descripcion.Value))
            .ToList();
}
