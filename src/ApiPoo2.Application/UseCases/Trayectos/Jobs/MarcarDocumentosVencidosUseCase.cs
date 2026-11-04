using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.TiposDocumento;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Trayectos.Jobs;

/// <summary>
///     Tarea programada: persiste el estado VENCIDO de los documentos cuya fecha de vencimiento ya
///     pasó. Las consultas siguen derivando el estado aunque el job todavía no haya corrido.
/// </summary>
public sealed class MarcarDocumentosVencidosUseCase : BaseUseCase<SinParametros, int>
{
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public MarcarDocumentosVencidosUseCase(
        IEnumerable<FluentValidation.IValidator<SinParametros>> validators,
        ILoggerFactory loggerFactory,
        IVehiculoRepository vehiculoRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _vehiculoRepository = vehiculoRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<int> ExecuteCoreAsync(
        SinParametros request,
        CancellationToken cancellationToken)
    {
        var ahora = _dateTimeProvider.UtcNow;
        var vehiculos = await _vehiculoRepository.GetAllWithDetailsForUpdateAsync(cancellationToken);
        var marcados = 0;

        foreach (var documento in vehiculos.SelectMany(v => v.GetTiposDocumento()))
        {
            if (documento.Estado == EstadoDocumento.Vencido
                || documento.EstadoActual(ahora) != EstadoDocumento.Vencido)
            {
                continue;
            }

            documento.MarcarVencido(ahora);
            marcados++;
        }

        if (marcados > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return marcados;
    }
}
