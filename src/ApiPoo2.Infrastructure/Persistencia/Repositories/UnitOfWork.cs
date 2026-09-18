using ApiPoo2.Application.Exceptions;
using ApiPoo2.Application.IRepositories;
using ApiPoo2.Domain.Common;
using ApiPoo2.Infrastructure.Persistencia.Exceptions;
using Microsoft.EntityFrameworkCore;

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

        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new UnauthorizedException("refresh.reuse", "Se detectó reuso de token de refresco. Sesión revocada.");
        }
        catch (DbUpdateException ex)
        {
            throw PersistenceErrors.Map(ex);
        }
    }
}