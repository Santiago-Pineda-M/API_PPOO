using ApiPoo2.Domain.Personas;

namespace ApiPoo2.Application.IRepositories;

public interface IConductorVehiculoRepository
{
    Task<IReadOnlyList<ConductorVehiculo>> GetByPersonaAsync(
        Guid personaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConductorVehiculo>> GetOperablesAsync(CancellationToken cancellationToken = default);

    Task<ConductorVehiculo?> GetByPersonaAndVehiculoAsync(
        Guid personaId,
        Guid vehiculoId,
        CancellationToken cancellationToken = default);

    void Add(ConductorVehiculo conductorVehiculo);
}
