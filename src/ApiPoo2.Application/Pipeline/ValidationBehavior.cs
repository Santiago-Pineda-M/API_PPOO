using ApiPoo2.Application.Exceptions;
using FluentValidation;

namespace ApiPoo2.Application.Pipeline;

public sealed class ValidationBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResult> Handle(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        var validators = _validators.ToList();

        if (validators.Count == 0)
        {
            return await next(cancellationToken);
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

        return await next(cancellationToken);
    }
}