namespace Herald.Samples.WebApi.Behaviors;

/// <summary>A request that can validate itself.</summary>
public interface IValidatable
{
    IEnumerable<string> Validate();
}

/// <summary>Thrown when a request fails validation.</summary>
public sealed class ValidationException(IEnumerable<string> errors) : Exception(string.Join(" ", errors));

/// <summary>
/// Rejects invalid requests before they reach their handler.
/// The generic constraint means Herald only applies it to requests implementing <see cref="IValidatable"/>.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IValidatable
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var errors = request.Validate().ToList();

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        return next();
    }
}
