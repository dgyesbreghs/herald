namespace Herald;

/// <summary>
/// Publishes notifications to all of their handlers.
/// </summary>
public interface IPublisher
{
    /// <summary>
    /// Publishes a notification to all registered handlers, one after another in registration order.
    /// </summary>
    /// <remarks>
    /// Handlers are resolved for the runtime type of <paramref name="notification"/>.
    /// Publishing without handlers is a no-op. The first handler that throws stops the remaining handlers.
    /// </remarks>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TNotification">The type of the notification.</typeparam>
    /// <returns>A task that completes when all handlers have run.</returns>
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
