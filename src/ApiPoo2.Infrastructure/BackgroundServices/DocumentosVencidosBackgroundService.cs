using ApiPoo2.Application.Common;
using ApiPoo2.Application.UseCases.Trayectos.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Infrastructure.BackgroundServices;

/// <summary>Tarea programada (cada 2 minutos) que persiste los documentos de vehículo vencidos.</summary>
public sealed class DocumentosVencidosBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentosVencidosBackgroundService> _logger;

    public DocumentosVencidosBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<DocumentosVencidosBackgroundService> logger)
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
                var useCase = scope.ServiceProvider.GetRequiredService<MarcarDocumentosVencidosUseCase>();
                var marcados = await useCase.ExecuteAsync(new SinParametros(), stoppingToken);

                if (marcados > 0)
                {
                    _logger.LogInformation(
                        "Se marcaron {Cantidad} documentos de vehículo como vencidos.",
                        marcados);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló la tarea de marcado de documentos vencidos.");
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
