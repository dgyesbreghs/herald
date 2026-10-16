namespace Herald;

/// <summary>
/// Sends requests to their single handler.
/// </summary>
public interface ISender
{
    /// <summary>
    /// Sends a request to its single handler.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <returns>The response of the handler.</returns>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a request without a response to its single handler.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the request has been handled.</returns>
    Task Send(IRequest request, CancellationToken cancellationToken = default);
}
