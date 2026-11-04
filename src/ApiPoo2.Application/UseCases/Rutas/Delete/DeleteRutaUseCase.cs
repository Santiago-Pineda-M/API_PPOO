using ApiPoo2.Application.UseCases.Auth;
using ApiPoo2.Domain.Rutas;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Rutas.Delete;

public sealed record DeleteRutaInputDto(string CodigoRuta);

public sealed class DeleteRutaUseCase : BaseUseCase<DeleteRutaInputDto, OperationResult>
{
    private readonly IRutaRepository _rutaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRutaUseCase(
        IEnumerable<FluentValidation.IValidator<DeleteRutaInputDto>> validators,
        ILoggerFactory loggerFactory,
        IRutaRepository rutaRepository,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _rutaRepository = rutaRepository;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<OperationResult> ExecuteCoreAsync(
        DeleteRutaInputDto request,
        CancellationToken cancellationToken)
    {
        var codigo = CodigoRuta.From(request.CodigoRuta);
        var ruta = await _rutaRepository.GetByCodigoAsync(codigo, cancellationToken)
            ?? throw new NotFoundException("ruta.not_found", "La ruta no existe.");

        _rutaRepository.Remove(ruta);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }
}
