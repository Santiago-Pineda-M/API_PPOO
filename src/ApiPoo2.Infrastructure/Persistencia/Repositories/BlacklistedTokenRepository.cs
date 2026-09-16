using ApiPoo2.Application.IRepositories;
using ApiPoo2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiPoo2.Infrastructure.Persistencia.Repositories;

public sealed class BlacklistedTokenRepository : GenericRepository<BlacklistedToken>, IBlacklistedTokenRepository
{
    public BlacklistedTokenRepository(AppDbContext dbContext)
        : base(dbContext)
    {
    }

    public Task<bool> IsBlacklistedAsync(Guid jti, CancellationToken cancellationToken = default)
        => DbSet.AsNoTracking()
            .AnyAsync(t => t.Jti == jti && t.ExpiresAtUtc > DateTime.UtcNow, cancellationToken);

    public async Task<int> PurgeExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var expired = await DbSet.Where(t => t.ExpiresAtUtc <= utcNow).ToListAsync(cancellationToken);
        DbSet.RemoveRange(expired);
        return expired.Count;
    }
}