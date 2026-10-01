using ApiPoo2.Application.IRepositories;
using ApiPoo2.Domain.Personas;
using Microsoft.EntityFrameworkCore;

namespace ApiPoo2.Infrastructure.Persistence.Repositories;

public sealed class ConductorVehiculoRepository : IConductorVehiculoRepository
{
    private readonly AppDbContext _dbContext;

    public ConductorVehiculoRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<ConductorVehiculo>> GetByPersonaAsync(
        Guid personaId,
        CancellationToken cancellationToken = default)
        => await _dbContext.ConductoresVehiculos.AsNoTracking()
            .Where(c => c.PersonaId == personaId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ConductorVehiculo>> GetOperablesAsync(
        CancellationToken cancellationToken = default)
        => await _dbContext.ConductoresVehiculos.AsNoTracking()
            .Where(c => c.Estado == EstadoConductor.Po)
            .ToListAsync(cancellationToken);

    public Task<ConductorVehiculo?> GetByPersonaAndVehiculoAsync(
        Guid personaId,
        Guid vehiculoId,
        CancellationToken cancellationToken = default)
        => _dbContext.ConductoresVehiculos
            .FirstOrDefaultAsync(c => c.PersonaId == personaId && c.VehiculoId == vehiculoId, cancellationToken);

    public void Add(ConductorVehiculo conductorVehiculo) => _dbContext.ConductoresVehiculos.Add(conductorVehiculo);
}
