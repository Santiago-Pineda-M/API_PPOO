using ApiPoo2.Application.IRepositories;
using ApiPoo2.Domain.Documentos;
using ApiPoo2.Domain.Vehiculos;
using Microsoft.EntityFrameworkCore;

namespace ApiPoo2.Infrastructure.Persistence.Repositories;

public sealed class DocumentoRepository : IDocumentoRepository
{
    private readonly AppDbContext _dbContext;

    public DocumentoRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<Documento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Documentos.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<Documento?> GetByCodigoAsync(DocumentoCodigo codigo, CancellationToken cancellationToken = default)
        => _dbContext.Documentos.FirstOrDefaultAsync(d => d.Codigo == codigo, cancellationToken);

    public async Task<IReadOnlyList<Documento>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Documentos.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<VehiculoDocumento>> GetByVehiculoAsync(
        Guid vehiculoId,
        CancellationToken cancellationToken = default)
        => await _dbContext.VehiculosDocumentos.AsNoTracking()
            .Where(d => d.VehiculoId == vehiculoId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<VehiculoDocumento>> GetVencidosAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
        => await _dbContext.VehiculosDocumentos.AsNoTracking()
            .Where(d => d.FechaVencimiento <= utcNow)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<VehiculoDocumento>> GetPorVencerAsync(
        DateTime utcNow,
        int dias,
        CancellationToken cancellationToken = default)
    {
        var limite = utcNow.AddDays(dias);

        return await _dbContext.VehiculosDocumentos.AsNoTracking()
            .Where(d => d.FechaVencimiento > utcNow && d.FechaVencimiento <= limite)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Vehiculo>> GetConDocumentoAsync(
        DocumentoCodigo codigo,
        CancellationToken cancellationToken = default)
    {
        var documentoId = await _dbContext.Documentos.AsNoTracking()
            .Where(d => d.Codigo == codigo)
            .Select(d => d.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (documentoId == Guid.Empty)
        {
            return [];
        }

        var vehiculoIds = await _dbContext.VehiculosDocumentos.AsNoTracking()
            .Where(d => d.DocumentoId == documentoId)
            .Select(d => d.VehiculoId)
            .ToListAsync(cancellationToken);

        return await _dbContext.Vehiculos.AsNoTracking()
            .Where(v => vehiculoIds.Contains(v.Id))
            .ToListAsync(cancellationToken);
    }

    public void Add(Documento documento) => _dbContext.Documentos.Add(documento);

    public void Remove(Documento documento) => _dbContext.Documentos.Remove(documento);

    public Task<bool> ExistsByDocumentoIdAsync(Guid documentoId, CancellationToken cancellationToken = default)
        => _dbContext.VehiculosDocumentos.AsNoTracking().AnyAsync(d => d.DocumentoId == documentoId, cancellationToken);
}
