using ApiPoo2.Domain.Documentos;
using ApiPoo2.Domain.Vehiculos;

namespace ApiPoo2.Application.IRepositories;

public interface IDocumentoRepository
{
    Task<Documento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Documento?> GetByCodigoAsync(DocumentoCodigo codigo, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Documento>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VehiculoDocumento>> GetByVehiculoAsync(
        Guid vehiculoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VehiculoDocumento>> GetVencidosAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VehiculoDocumento>> GetPorVencerAsync(
        DateTime utcNow,
        int dias,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Vehiculo>> GetConDocumentoAsync(
        DocumentoCodigo codigo,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByDocumentoIdAsync(
        Guid documentoId,
        CancellationToken cancellationToken = default);

    void Add(Documento documento);

    void Remove(Documento documento);
}
