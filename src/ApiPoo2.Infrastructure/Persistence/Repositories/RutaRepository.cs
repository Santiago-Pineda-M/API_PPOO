using ApiPoo2.Application.IRepositories;
using ApiPoo2.Domain.Rutas;
using ApiPoo2.Domain.Vehiculos;
using Microsoft.EntityFrameworkCore;

namespace ApiPoo2.Infrastructure.Persistence.Repositories;

public sealed class RutaRepository : GenericRepository<Ruta>, IRutaRepository
{
    public RutaRepository(AppDbContext dbContext)
        : base(dbContext)
    {
    }

    public Task<Ruta?> GetByCodigoAsync(CodigoRuta codigo, CancellationToken cancellationToken = default)
        => ConDetalle().FirstOrDefaultAsync(r => r.Codigo == codigo, cancellationToken);

    public override Task<Ruta?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => ConDetalle().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<Ruta?> GetByParadaAsync(Guid paradaId, CancellationToken cancellationToken = default)
        => ConDetalle().FirstOrDefaultAsync(
            r => r.Paradas.Any(p => p.Id == paradaId),
            cancellationToken);

    public void AddParada(Parada parada) => DbContext.Set<Parada>().Add(parada);

    public Task<bool> ExistsAsync(CodigoRuta codigo, CancellationToken cancellationToken = default)
        => DbSet.AsNoTracking().AnyAsync(r => r.Codigo == codigo, cancellationToken);

    public async Task<IReadOnlyList<Ruta>> GetAllAsync(CancellationToken cancellationToken = default)
        => await ConDetalle()
            .OrderBy(r => r.Codigo)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Ruta>> GetByPersonaAsync(
        Guid personaId,
        CancellationToken cancellationToken = default)
        => await ConDetalle()
            .Where(r => r.PersonaId == personaId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Ruta>> GetByPlacaAsync(
        Placa placa,
        CancellationToken cancellationToken = default)
        => await ConDetalle()
            .Where(r => r.Vehiculo.Placa == placa)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ParadaPendienteGeocodificar>> GetPendientesCoordenadasAsync(
        int limite,
        CancellationToken cancellationToken = default)
        => await DbContext.Set<Parada>().AsNoTracking()
            .Where(p => p.Latitud == null || p.Longitud == null)
            .OrderBy(p => p.CreatedAtUtc)
            .Take(limite)
            .Select(p => new ParadaPendienteGeocodificar(
                p.IdRuta,
                p.Id,
                p.Ubicacion.Value,
                p.CreatedAtUtc))
            .ToListAsync(cancellationToken);

    private IQueryable<Ruta> ConDetalle()
        => DbSet
            .Include(r => r.Paradas)
            .Include(r => r.Persona)
                .ThenInclude(p => p.ConductoresVehiculosAssociated)
            .Include(r => r.Vehiculo)
                .ThenInclude(v => v.DocumentosAssociated)
            .AsSplitQuery();
}
