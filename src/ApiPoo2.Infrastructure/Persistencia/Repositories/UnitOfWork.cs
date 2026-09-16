using ApiPoo2.Application.IRepositories;
using ApiPoo2.Domain.Common;

namespace ApiPoo2.Infrastructure.Persistencia.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public UnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in _dbContext.ChangeTracker.Entries<IHasDomainEvents>())
        {
            entry.Entity.ClearDomainEvents();
        }

        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}