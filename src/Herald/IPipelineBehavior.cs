namespace Herald;

/// <summary>
/// Invokes the next step in the pipeline: the next behavior, or the request handler itself.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <returns>The response.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Wraps the handling of a request, e.g. for logging, validation or transactions.
/// </summary>
/// <remarks>
/// Requests without a response (<see cref="IRequest"/>) flow through behaviors with <see cref="Unit"/> as <typeparamref name="TResponse"/>.
/// Behaviors run in registration order: the first registered behavior is the outermost.
/// </remarks>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Handles the request, calling <paramref name="next"/> to continue the pipeline.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="next">The next step in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
