namespace ApiPoo2.Application.Pipeline;

public delegate Task<TResult> RequestHandlerDelegate<TResult>(CancellationToken cancellationToken);

public interface IPipelineBehavior<TRequest, TResult>
{
    Task<TResult> Handle(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken);
}