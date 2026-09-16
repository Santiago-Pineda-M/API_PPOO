using System.Linq.Expressions;
using ApiPoo2.Application.IRepositories;
using ApiPoo2.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace ApiPoo2.Infrastructure.Persistencia.Repositories;

public class GenericRepository<T> : IRepository<T>
    where T : BaseEntity
{
    protected readonly AppDbContext DbContext;
    protected readonly DbSet<T> DbSet;

    public GenericRepository(AppDbContext dbContext)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<T>();
    }

    public virtual Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => DbSet.FindAsync([id], cancellationToken).AsTask();

    public virtual Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => DbSet.AsNoTracking().AnyAsync(predicate, cancellationToken);

    public virtual void Add(T entity) => DbSet.Add(entity);

    public virtual void Update(T entity) => DbSet.Update(entity);

    public virtual void Remove(T entity) => DbSet.Remove(entity);
}