using ApiPoo2.Application.Common;
using ApiPoo2.Application.UseCases.Trayectos.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Infrastructure.BackgroundServices;

/// <summary>Tarea programada (cada 90 segundos) que geocodifica las paradas sin coordenadas.</summary>
public sealed class GeocodificacionBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(90);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GeocodificacionBackgroundService> _logger;

    public GeocodificacionBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<GeocodificacionBackgroundService> logger)
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
                var useCase = scope.ServiceProvider.GetRequiredService<GeocodificarParadasUseCase>();
                var asignadas = await useCase.ExecuteAsync(new SinParametros(), stoppingToken);

                if (asignadas > 0)
                {
                    _logger.LogInformation(
                        "Se geocodificaron {Cantidad} paradas pendientes.",
                        asignadas);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló la tarea de geocodificación de paradas.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
