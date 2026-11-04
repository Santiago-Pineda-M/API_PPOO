using ApiPoo2.Domain.Trayectos;

namespace ApiPoo2.Application.IRepositories;

public sealed record CargueResumen(
    long IdCargue,
    DateTime FechaCargue,
    int Total,
    int Cargados,
    int Validados,
    int Procesados,
    int Errores);

public interface ITrayectoTemporalRepository
{
    Task<bool> ExisteCargueAsync(long idCargue, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrayectoTemporal>> GetByIdCargueAsync(
        long idCargue,
        EstadoTrayectoTemporal? estado = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CargueResumen>> GetResumenesAsync(CancellationToken cancellationToken = default);

    Task<int> EliminarCargueAsync(long idCargue, CancellationToken cancellationToken = default);
}
