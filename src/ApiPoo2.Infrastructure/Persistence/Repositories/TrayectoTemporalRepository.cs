using ApiPoo2.Application.IRepositories;
using ApiPoo2.Domain.Trayectos;
using Microsoft.EntityFrameworkCore;

namespace ApiPoo2.Infrastructure.Persistence.Repositories;

public sealed class TrayectoTemporalRepository : ITrayectoTemporalRepository
{
    private readonly AppDbContext _dbContext;

    public TrayectoTemporalRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<bool> ExisteCargueAsync(long idCargue, CancellationToken cancellationToken = default)
        => _dbContext.TrayectosTemporales
            .AsNoTracking()
            .AnyAsync(t => t.IdCargue == idCargue, cancellationToken);

    public async Task<IReadOnlyList<TrayectoTemporal>> GetByIdCargueAsync(
        long idCargue,
        EstadoTrayectoTemporal? estado = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TrayectosTemporales
            .AsNoTracking()
            .Where(t => t.IdCargue == idCargue);

        if (estado.HasValue)
        {
            query = query.Where(t => t.Estado == estado.Value);
        }

        return await query
            .OrderBy(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CargueResumen>> GetResumenesAsync(
        CancellationToken cancellationToken = default)
    {
        // La proyección directa al record no es traducible; el tipo anónimo sí lo es.
        var filas = await _dbContext.TrayectosTemporales
            .AsNoTracking()
            .GroupBy(t => t.IdCargue)
            .Select(g => new
            {
                IdCargue = g.Key,
                FechaCargue = g.Min(t => t.CreatedAtUtc),
                Total = g.Count(),
                Cargados = g.Count(t => t.Estado == EstadoTrayectoTemporal.Cargado),
                Validados = g.Count(t => t.Estado == EstadoTrayectoTemporal.Validado),
                Procesados = g.Count(t => t.Estado == EstadoTrayectoTemporal.Procesado),
                Errores = g.Count(t => t.Estado == EstadoTrayectoTemporal.Error),
            })
            .OrderByDescending(r => r.IdCargue)
            .ToListAsync(cancellationToken);

        return filas
            .Select(f => new CargueResumen(
                f.IdCargue,
                f.FechaCargue,
                f.Total,
                f.Cargados,
                f.Validados,
                f.Procesados,
                f.Errores))
            .ToList();
    }

    public Task<int> EliminarCargueAsync(long idCargue, CancellationToken cancellationToken = default)
        => _dbContext.TrayectosTemporales
            .Where(t => t.IdCargue == idCargue)
            .ExecuteDeleteAsync(cancellationToken);
}
