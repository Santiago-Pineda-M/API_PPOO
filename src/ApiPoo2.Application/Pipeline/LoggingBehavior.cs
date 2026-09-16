using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.Pipeline;

public sealed class LoggingBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResult>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResult>> logger)
    {
        _logger = logger;
    }

    public async Task<TResult> Handle(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Iniciando {RequestName}", requestName);

        var sw = Stopwatch.StartNew();

        try
        {
            var result = await next(cancellationToken);
            sw.Stop();
            _logger.LogInformation("Finalizado {RequestName} en {ElapsedMs} ms", requestName, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Falló {RequestName} en {ElapsedMs} ms", requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}