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

    void Add(Vehiculo vehiculo);

    void Remove(Vehiculo vehiculo);
}
