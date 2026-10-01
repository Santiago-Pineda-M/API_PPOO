using ApiPoo2.Application.Exceptions;
using ApiPoo2.Application.IRepositories;
using ApiPoo2.Infrastructure.Persistence.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ApiPoo2.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public UnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
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
