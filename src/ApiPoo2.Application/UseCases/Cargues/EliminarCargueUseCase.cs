using ApiPoo2.Application.UseCases.Auth;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Cargues;

public sealed class EliminarCargueUseCase
    : BaseUseCase<EliminarCargueInputDto, OperationResult>
{
    private readonly ITrayectoTemporalRepository _trayectoRepository;

    public EliminarCargueUseCase(
        IEnumerable<FluentValidation.IValidator<EliminarCargueInputDto>> validators,
        ILoggerFactory loggerFactory,
        ITrayectoTemporalRepository trayectoRepository)
        : base(validators, loggerFactory)
    {
        _trayectoRepository = trayectoRepository;
    }

    protected override async Task<OperationResult> ExecuteCoreAsync(
        EliminarCargueInputDto request,
        CancellationToken cancellationToken)
    {
        var eliminadas = await _trayectoRepository.EliminarCargueAsync(
            request.IdCargue,
            cancellationToken);

        if (eliminadas == 0)
        {
            throw new NotFoundException("cargue.not_found", "El cargue no existe.");
        }

        return OperationResult.Success();
    }
}
