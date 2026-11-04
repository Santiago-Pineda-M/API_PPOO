using ApiPoo2.Domain.Trayectos;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Cargues;

public sealed class ListarCarguesUseCase
    : BaseUseCase<ListarCarguesInputDto, IReadOnlyList<CargueResumenOutputDto>>
{
    private readonly ITrayectoTemporalRepository _trayectoRepository;

    public ListarCarguesUseCase(
        IEnumerable<FluentValidation.IValidator<ListarCarguesInputDto>> validators,
        ILoggerFactory loggerFactory,
        ITrayectoTemporalRepository trayectoRepository)
        : base(validators, loggerFactory)
    {
        _trayectoRepository = trayectoRepository;
    }

    protected override async Task<IReadOnlyList<CargueResumenOutputDto>> ExecuteCoreAsync(
        ListarCarguesInputDto request,
        CancellationToken cancellationToken)
    {
        var resumenes = await _trayectoRepository.GetResumenesAsync(cancellationToken);

        return resumenes
            .Where(r => request.Estado is null || Contar(r, request.Estado.Value) > 0)
            .OrderByDescending(r => r.IdCargue)
            .Select(r => new CargueResumenOutputDto(
                r.IdCargue,
                r.FechaCargue,
                r.Total,
                r.Cargados,
                r.Validados,
                r.Procesados,
                r.Errores))
            .ToList();
    }

    private static int Contar(CargueResumen resumen, EstadoTrayectoTemporal estado) => estado switch
    {
        EstadoTrayectoTemporal.Cargado => resumen.Cargados,
        EstadoTrayectoTemporal.Validado => resumen.Validados,
        EstadoTrayectoTemporal.Procesado => resumen.Procesados,
        EstadoTrayectoTemporal.Error => resumen.Errores,
        _ => 0,
    };
}
