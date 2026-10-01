using ApiPoo2.Domain.Personas;

namespace ApiPoo2.Application.IRepositories;

public interface IPersonaRepository : IRepository<Persona>
{
    Task<Persona?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Persona?> GetByNumeroIdentificacionAsync(
        NumeroIdentificacion numero,
        CancellationToken cancellationToken = default);

    Task<Persona?> GetByCorreoAsync(
        CorreoElectronico correo,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNumeroIdentificacionAsync(
        NumeroIdentificacion numero,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByLoginAsync(Login login, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<TipoPersona, int>> CountByTipoAsync(CancellationToken cancellationToken = default);
}
