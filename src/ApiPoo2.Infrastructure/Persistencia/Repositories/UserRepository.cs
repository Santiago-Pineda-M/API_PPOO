using ApiPoo2.Application.IRepositories;
using ApiPoo2.Domain.Entities;
using ApiPoo2.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ApiPoo2.Infrastructure.Persistencia.Repositories;

public sealed class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext dbContext)
        : base(dbContext)
    {
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var target = Email.From(email);
        return DbSet.FirstOrDefaultAsync(u => u.Email == target, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var target = Email.From(email);
        return DbSet.AsNoTracking().AnyAsync(u => u.Email == target, cancellationToken);
    }

    public Task<User?> GetByIdWithRefreshTokensAsync(Guid id, CancellationToken cancellationToken = default)
        => DbSet.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
}