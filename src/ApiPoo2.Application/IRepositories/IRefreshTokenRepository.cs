using ApiPoo2.Domain.Entities;

namespace ApiPoo2.Application.IRepositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<int> DeleteExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}