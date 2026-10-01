using ApiPoo2.Application.IRepositories;
using ApiPoo2.Domain.TiposDocumento;
using ApiPoo2.Domain.Vehiculos;
using Microsoft.EntityFrameworkCore;

namespace ApiPoo2.Infrastructure.Persistence.Repositories;

public sealed class TipoDocumentoRepository : ITipoDocumentoRepository
{
    private readonly AppDbContext _dbContext;

    public TipoDocumentoRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<TipoDocumento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.TiposDocumento.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<TipoDocumento?> GetByCodigoAsync(TipoDocumentoCodigo codigo, CancellationToken cancellationToken = default)
        => _dbContext.TiposDocumento.FirstOrDefaultAsync(d => d.Codigo == codigo, cancellationToken);

    public async Task<IReadOnlyList<TipoDocumento>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.TiposDocumento.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DocumentoVehiculo>> GetByVehiculoAsync(
        Guid vehiculoId,
        CancellationToken cancellationToken = default)
        => await _dbContext.DocumentosVehiculo.AsNoTracking()
            .Where(d => d.VehiculoId == vehiculoId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DocumentoVehiculo>> GetVencidosAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
        => await _dbContext.DocumentosVehiculo.AsNoTracking()
            .Where(d => d.FechaVencimiento <= utcNow)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DocumentoVehiculo>> GetPorVencerAsync(
        DateTime utcNow,
        int dias,
        CancellationToken cancellationToken = default)
    {
        var limite = utcNow.AddDays(dias);

        return await _dbContext.DocumentosVehiculo.AsNoTracking()
            .Where(d => d.FechaVencimiento > utcNow && d.FechaVencimiento <= limite)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Vehiculo>> GetConTipoDocumentoAsync(
        TipoDocumentoCodigo codigo,
        CancellationToken cancellationToken = default)
    {
        var tipoDocumentoId = await _dbContext.TiposDocumento.AsNoTracking()
            .Where(d => d.Codigo == codigo)
            .Select(d => d.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (tipoDocumentoId == Guid.Empty)
        {
            return [];
        }

        var vehiculoIds = await _dbContext.DocumentosVehiculo.AsNoTracking()
            .Where(d => d.TipoDocumentoId == tipoDocumentoId)
            .Select(d => d.VehiculoId)
            .ToListAsync(cancellationToken);

        return await _dbContext.Vehiculos.AsNoTracking()
            .Where(v => vehiculoIds.Contains(v.Id))
            .ToListAsync(cancellationToken);
    }

    public void Add(TipoDocumento documento) => _dbContext.TiposDocumento.Add(documento);

    public void Remove(TipoDocumento documento) => _dbContext.TiposDocumento.Remove(documento);

    public Task<bool> ExistsByTipoDocumentoIdAsync(Guid tipoDocumentoId, CancellationToken cancellationToken = default)
        => _dbContext.DocumentosVehiculo.AsNoTracking().AnyAsync(d => d.TipoDocumentoId == tipoDocumentoId, cancellationToken);
}
