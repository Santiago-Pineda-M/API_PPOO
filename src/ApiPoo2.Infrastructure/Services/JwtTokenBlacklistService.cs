using ApiPoo2.Application.IRepositories;
using ApiPoo2.Application.IServices;
using ApiPoo2.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace ApiPoo2.Infrastructure.Services;

public sealed class JwtTokenBlacklistService : IJwtTokenBlacklistService
{
    private const string CacheKeyPrefix = "jwt:blacklist:";

    private readonly IBlacklistedTokenRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly IDateTimeProvider _dateTimeProvider;

    public JwtTokenBlacklistService(
        IBlacklistedTokenRepository repository,
        IUnitOfWork unitOfWork,
        IMemoryCache cache,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<bool> IsBlacklistedAsync(Guid jti, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyPrefix + jti;

        if (_cache.TryGetValue<bool>(cacheKey, out var cached))
        {
            return cached;
        }

        var blacklisted = await _repository.IsBlacklistedAsync(jti, cancellationToken);
        _cache.Set(cacheKey, blacklisted, TimeSpan.FromMinutes(5));
        return blacklisted;
    }

    public async Task BlacklistAsync(Guid jti, Guid userId, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;

        if (expiresAtUtc <= now)
        {
            return;
        }

        var entry = BlacklistedToken.Create(jti, userId, expiresAtUtc, now);
        _repository.Add(entry);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cache.Set(CacheKeyPrefix + jti, true, expiresAtUtc - now);
    }

    public async Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default)
    {
        var removed = await _repository.PurgeExpiredAsync(_dateTimeProvider.UtcNow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return removed;
    }
}