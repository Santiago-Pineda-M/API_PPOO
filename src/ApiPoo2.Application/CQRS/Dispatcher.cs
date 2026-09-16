using ApiPoo2.Application.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPoo2.Application.CQRS;

internal sealed class Dispatcher : IDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>
    {
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
        return ExecutePipelineAsync(command, handler.Handle, cancellationToken);
    }

    public Task<TResult> QueryAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
    {
        var handler = _serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        return ExecutePipelineAsync(query, handler.Handle, cancellationToken);
    }

    private Task<TResult> ExecutePipelineAsync<TRequest, TResult>(
        TRequest request,
        Func<TRequest, CancellationToken, Task<TResult>> handle,
        CancellationToken cancellationToken)
    {
        var behaviors = _serviceProvider.GetServices<IPipelineBehavior<TRequest, TResult>>().ToList();

        RequestHandlerDelegate<TResult> next = ct => handle(request, ct);

        for (var i = behaviors.Count - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var inner = next;
            next = ct => behavior.Handle(request, inner, ct);
        }

        return next(cancellationToken);
    }
}

public static class DispatcherExtensions
{
    public static Task<TResult> SendAsync<TCommand, TResult>(this IServiceProvider services, TCommand command, CancellationToken ct = default)
        where TCommand : ICommand<TResult>
        => services.GetRequiredService<IDispatcher>().SendAsync<TCommand, TResult>(command, ct);
}