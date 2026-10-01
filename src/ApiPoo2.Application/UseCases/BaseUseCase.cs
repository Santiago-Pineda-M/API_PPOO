using System.Diagnostics;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases;

public abstract class BaseUseCase<TRequest, TResult> : IUseCase<TRequest, TResult>
    where TRequest : class
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger _logger;

    protected BaseUseCase(IEnumerable<IValidator<TRequest>> validators, ILoggerFactory loggerFactory)
    {
        _validators = validators;
        _logger = loggerFactory.CreateLogger(GetType());
    }

    public async Task<TResult> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var useCaseName = GetType().Name;
        _logger.LogInformation("Iniciando {UseCase}", useCaseName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await ValidateAsync(request, cancellationToken);
            var result = await ExecuteCoreAsync(request, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation("Finalizado {UseCase} en {ElapsedMs} ms", useCaseName, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Falló {UseCase} en {ElapsedMs} ms", useCaseName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    protected abstract Task<TResult> ExecuteCoreAsync(TRequest request, CancellationToken cancellationToken);

    private async Task ValidateAsync(TRequest request, CancellationToken cancellationToken)
    {
        var validators = _validators.ToList();

        if (validators.Count == 0)
        {
            return;
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = results
            .SelectMany(r => r.Errors)
            .Where(e => e is not null)
            .Select(e => e.ErrorMessage)
            .Distinct()
            .ToList();

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }
}
