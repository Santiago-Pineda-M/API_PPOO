using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Users;

namespace ApiPoo2.Application.IRepositories;

/// <summary>
///     La PK de <see cref="User" /> es compuesta (idpersona, login), así que este repositorio no
///     implementa el <c>IRepository&lt;T&gt;</c> genérico: no existe un GetByIdAsync(Guid).
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByPersonaAndLoginAsync(
        Guid idPersona,
        Login login,
        CancellationToken cancellationToken = default);

    Task<User?> GetByLoginAsync(Login login, CancellationToken cancellationToken = default);

    /// <summary>Resuelve la persona junto con su usuario, incluyendo los refresh tokens.</summary>
    Task<Persona?> GetPersonaByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByApiKeyAsync(ApiKey apiKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetByPersonaAsync(Guid idPersona, CancellationToken cancellationToken = default);

    void Add(User user);
}
