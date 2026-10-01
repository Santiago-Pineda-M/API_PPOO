using ApiPoo2.Domain.BlacklistedTokens;

namespace ApiPoo2.Application.IRepositories;

public interface IBlacklistedTokenRepository : IRepository<BlacklistedToken>
{
    Task<bool> IsBlacklistedAsync(Guid jti, CancellationToken cancellationToken = default);

    Task<int> PurgeExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
