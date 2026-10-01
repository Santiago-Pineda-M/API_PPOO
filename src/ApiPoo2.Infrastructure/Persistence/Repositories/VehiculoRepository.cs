using ApiPoo2.Application.IRepositories;
using ApiPoo2.Domain.Vehiculos;
using Microsoft.EntityFrameworkCore;

namespace ApiPoo2.Infrastructure.Persistence.Repositories;

public sealed class VehiculoRepository : GenericRepository<Vehiculo>, IVehiculoRepository
{
    public VehiculoRepository(AppDbContext dbContext)
        : base(dbContext)
    {
    }

    public Task<Vehiculo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => DbSet.Include(v => v.DocumentosAssociated).Include(v => v.ConductoresAssociated).ThenInclude(c => c.Persona)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<Vehiculo?> GetByPlacaAsync(Placa placa, CancellationToken cancellationToken = default)
        => DbSet.Include(v => v.DocumentosAssociated).Include(v => v.ConductoresAssociated).ThenInclude(c => c.Persona)
            .FirstOrDefaultAsync(v => v.Placa == placa, cancellationToken);

    public async Task<IReadOnlyList<Vehiculo>> GetByTipoAsync(
        TipoVehiculo tipo,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Include(v => v.DocumentosAssociated)
            .Include(v => v.ConductoresAssociated).ThenInclude(c => c.Persona)
            .AsSplitQuery()
            .Where(v => v.TipoVehiculo == tipo)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Vehiculo>> GetAllWithDetailsAsync(
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Include(v => v.DocumentosAssociated)
            .Include(v => v.ConductoresAssociated).ThenInclude(c => c.Persona)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
}
