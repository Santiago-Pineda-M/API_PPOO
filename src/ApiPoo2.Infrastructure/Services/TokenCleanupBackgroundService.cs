using ApiPoo2.Application.IRepositories;
using ApiPoo2.Application.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Infrastructure.Services;

public sealed class TokenCleanupBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TokenCleanupBackgroundService> _logger;

    public TokenCleanupBackgroundService(IServiceScopeFactory scopeFactory, ILogger<TokenCleanupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var blacklist = scope.ServiceProvider.GetRequiredService<IJwtTokenBlacklistService>();
                var refreshTokens = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var purgedBlacklist = await blacklist.PurgeExpiredAsync(stoppingToken);
                var removedRefreshTokens = await refreshTokens.DeleteExpiredAsync(DateTime.UtcNow, stoppingToken);
                await unitOfWork.SaveChangesAsync(stoppingToken);

                if (purgedBlacklist > 0 || removedRefreshTokens > 0)
                {
                    _logger.LogInformation(
                        "Limpieza de tokens completada: {Blacklisted} JWT y {RefreshTokens} refresh tokens purgados.",
                        purgedBlacklist,
                        removedRefreshTokens);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló la limpieza periódica de tokens.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}