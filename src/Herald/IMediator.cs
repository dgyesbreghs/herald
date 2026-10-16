namespace Herald;

/// <summary>
/// Dispatches requests to their <see cref="IRequestHandler{TRequest,TResponse}"/>.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Sends a request to its single handler.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <returns>The response of the handler.</returns>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
