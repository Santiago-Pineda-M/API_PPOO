namespace ApiPoo2.Application.IServices;

public interface IJwtTokenBlacklistService
{
    Task<bool> IsBlacklistedAsync(Guid jti, CancellationToken cancellationToken = default);

    Task BlacklistAsync(Guid jti, Guid userId, DateTime expiresAtUtc, CancellationToken cancellationToken = default);

    Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default);
}