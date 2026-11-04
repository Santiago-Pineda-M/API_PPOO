using ApiPoo2.Application.UseCases.Auth;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Paradas.Delete;

public sealed record DeleteParadaInputDto(Guid ParadaId);

public sealed class DeleteParadaUseCase : BaseUseCase<DeleteParadaInputDto, OperationResult>
{
    private readonly IRutaRepository _rutaRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteParadaUseCase(
        IEnumerable<FluentValidation.IValidator<DeleteParadaInputDto>> validators,
        ILoggerFactory loggerFactory,
        IRutaRepository rutaRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _rutaRepository = rutaRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<OperationResult> ExecuteCoreAsync(
        DeleteParadaInputDto request,
        CancellationToken cancellationToken)
    {
        var ruta = await _rutaRepository.GetByParadaAsync(request.ParadaId, cancellationToken)
            ?? throw new NotFoundException("parada.not_found", "La parada no existe.");

        var parada = ruta.BuscarParada(request.ParadaId)
            ?? throw new NotFoundException("parada.not_found", "La parada no existe.");

        if (parada.EsInicial())
        {
            throw new ConflictException(
                "parada.inicial.requerida",
                "No se puede eliminar la parada inicial; eliminá la ruta completa.");
        }

        ruta.EliminarParada(request.ParadaId, _dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }
}
