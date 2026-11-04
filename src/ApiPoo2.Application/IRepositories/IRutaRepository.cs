using ApiPoo2.Domain.Rutas;
using ApiPoo2.Domain.Vehiculos;

namespace ApiPoo2.Application.IRepositories;

/// <summary>Parada pendiente de geocodificar, con la ruta a la que pertenece.</summary>
public sealed record ParadaPendienteGeocodificar(
    Guid RutaId,
    Guid ParadaId,
    string Ubicacion,
    DateTime CreadaUtc);

public interface IRutaRepository
{
    Task<Ruta?> GetByCodigoAsync(CodigoRuta codigo, CancellationToken cancellationToken = default);

    Task<Ruta?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Ruta?> GetByParadaAsync(Guid paradaId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(CodigoRuta codigo, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Ruta>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Ruta>> GetByPersonaAsync(Guid personaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Ruta>> GetByPlacaAsync(Placa placa, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParadaPendienteGeocodificar>> GetPendientesCoordenadasAsync(
        int limite,
        CancellationToken cancellationToken = default);

    void Add(Ruta ruta);

    /// <summary>
    ///     Marca una parada nueva como alta explícita. EF, al descubrirla en la colección del
    ///     agregado con la clave ya generada, la trataría como existente y emitiría un UPDATE.
    /// </summary>
    void AddParada(Parada parada);

    void Remove(Ruta ruta);
}
