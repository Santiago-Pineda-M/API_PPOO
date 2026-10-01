using ApiPoo2.Application.UseCases.Auth;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.TiposDocumento;

public sealed class DeleteTipoDocumentoUseCase : BaseUseCase<DeleteTipoDocumentoInputDto, OperationResult>
{
    private readonly ITipoDocumentoRepository _documentoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTipoDocumentoUseCase(
        IEnumerable<IValidator<DeleteTipoDocumentoInputDto>> validators,
        ILoggerFactory loggerFactory,
        ITipoDocumentoRepository documentoRepository,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _documentoRepository = documentoRepository;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<OperationResult> ExecuteCoreAsync(
        DeleteTipoDocumentoInputDto request,
        CancellationToken cancellationToken)
    {
        var documento = await _documentoRepository.GetByIdAsync(request.TipoDocumentoId, cancellationToken)
            ?? throw new NotFoundException("document.not_found", "El documento no existe.");

        if (await _documentoRepository.ExistsByTipoDocumentoIdAsync(documento.Id, cancellationToken))
        {
            throw new ConflictException(
                "document.has_files",
                "No se puede eliminar un documento que ya tiene archivos asociados.");
        }

        _documentoRepository.Remove(documento);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }
}
