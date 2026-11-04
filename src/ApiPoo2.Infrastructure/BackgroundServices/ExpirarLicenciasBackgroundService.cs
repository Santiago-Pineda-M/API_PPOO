using ApiPoo2.Application.Common;
using ApiPoo2.Application.UseCases.Trayectos.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Infrastructure.BackgroundServices;

/// <summary>Tarea programada (cada 2 minutos) que restringe conductores con licencia vencida.</summary>
public sealed class ExpirarLicenciasBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpirarLicenciasBackgroundService> _logger;

    public ExpirarLicenciasBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpirarLicenciasBackgroundService> logger)
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
                var useCase = scope.ServiceProvider.GetRequiredService<ExpirarLicenciasConductoresUseCase>();
                var restringidos = await useCase.ExecuteAsync(new SinParametros(), stoppingToken);

                if (restringidos > 0)
                {
                    _logger.LogInformation(
                        "Se restringieron {Cantidad} asociaciones por licencias vencidas.",
                        restringidos);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló la tarea de expiración de licencias.");
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
