using ApiPoo2.Application.Common;
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

    /// <summary>
    ///     Personas que tienen un usuario asociado, con paginación (para el listado de usuarios).
    /// </summary>
    Task<(IReadOnlyList<Persona> Items, int Total)> GetPersonasConUsuarioPaginadoAsync(
        PagedFilter filtro,
        CancellationToken cancellationToken = default);

    Task<int> CountPersonasConUsuarioAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Conductores cuya licencia ya venció, con sus asociaciones a vehículos para que la tarea
    ///     programada pueda restringirlos (estado RO).
    /// </summary>
    Task<IReadOnlyList<Persona>> GetConLicenciaVencidaAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
