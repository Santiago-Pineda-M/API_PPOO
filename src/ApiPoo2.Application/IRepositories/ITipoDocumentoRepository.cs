using ApiPoo2.Domain.TiposDocumento;
using ApiPoo2.Domain.Vehiculos;

namespace ApiPoo2.Application.IRepositories;

public interface ITipoDocumentoRepository
{
    Task<TipoDocumento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TipoDocumento?> GetByCodigoAsync(TipoDocumentoCodigo codigo, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TipoDocumento>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentoVehiculo>> GetByVehiculoAsync(
        Guid vehiculoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentoVehiculo>> GetVencidosAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentoVehiculo>> GetPorVencerAsync(
        DateTime utcNow,
        int dias,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Vehiculo>> GetConTipoDocumentoAsync(
        TipoDocumentoCodigo codigo,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByTipoDocumentoIdAsync(
        Guid tipoDocumentoId,
        CancellationToken cancellationToken = default);

    void Add(TipoDocumento documento);

    void Remove(TipoDocumento documento);
}
