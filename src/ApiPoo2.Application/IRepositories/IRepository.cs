using System.Linq.Expressions;
using ApiPoo2.Domain.Common;

namespace ApiPoo2.Application.IRepositories;

/// <summary>
///     Repositorio genérico para entidades con identidad <see cref="Guid" />.
///     <see cref="Users.User" /> queda afuera porque su primary key es compuesta.
/// </summary>
public interface IRepository<T>
    where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    void Add(T entity);

    void Update(T entity);

    void Remove(T entity);
}
