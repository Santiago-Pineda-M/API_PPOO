using ApiPoo2.Application.UseCases.Auth;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Documentos;

public sealed class DeleteDocumentoUseCase : BaseUseCase<DeleteDocumentoInputDto, OperationResult>
{
    private readonly IDocumentoRepository _documentoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDocumentoUseCase(
        IEnumerable<IValidator<DeleteDocumentoInputDto>> validators,
        ILoggerFactory loggerFactory,
        IDocumentoRepository documentoRepository,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _documentoRepository = documentoRepository;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<OperationResult> ExecuteCoreAsync(
        DeleteDocumentoInputDto request,
        CancellationToken cancellationToken)
    {
        var documento = await _documentoRepository.GetByIdAsync(request.DocumentoId, cancellationToken)
            ?? throw new NotFoundException("document.not_found", "El documento no existe.");

        if (await _documentoRepository.ExistsByDocumentoIdAsync(documento.Id, cancellationToken))
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
