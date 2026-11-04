using ApiPoo2.Domain.Vehiculos;

namespace ApiPoo2.Application.IRepositories;

public interface IVehiculoRepository
{
    Task<Vehiculo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Vehiculo?> GetByPlacaAsync(Placa placa, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Vehiculo>> GetByTipoAsync(
        TipoVehiculo tipo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Vehiculo>> GetAllWithDetailsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Igual que <see cref="GetAllWithDetailsAsync" /> pero con seguimiento de cambios, para que la
    ///     tarea programada pueda marcar documentos vencidos y persistirlos.
    /// </summary>
    Task<IReadOnlyList<Vehiculo>> GetAllWithDetailsForUpdateAsync(
        CancellationToken cancellationToken = default);

    void Add(Vehiculo vehiculo);

    void Remove(Vehiculo vehiculo);
}
