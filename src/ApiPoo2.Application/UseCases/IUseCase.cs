namespace ApiPoo2.Application.UseCases;

public interface IUseCase<in TRequest, TResult>
    where TRequest : class
{
    Task<TResult> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}
