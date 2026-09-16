using ApiPoo2.Application.IRepositories;
using ApiPoo2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiPoo2.Infrastructure.Persistencia.Repositories;

public sealed class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AppDbContext dbContext)
        : base(dbContext)
    {
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => DbSet.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task<int> DeleteExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var expired = await DbSet.Where(t => t.ExpiresAtUtc < utcNow).ToListAsync(cancellationToken);
        DbSet.RemoveRange(expired);
        return expired.Count;
    }
}