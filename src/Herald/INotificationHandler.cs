namespace Herald;

/// <summary>
/// Handles a published <typeparamref name="TNotification"/>.
/// </summary>
/// <typeparam name="TNotification">The type of the notification.</typeparam>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Handles the notification.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the notification has been handled.</returns>
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
