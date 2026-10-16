using Microsoft.Extensions.DependencyInjection;

namespace Herald.Internal;

internal abstract class NotificationHandlerWrapper
{
    public abstract Task Handle(INotification notification, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

internal sealed class NotificationHandlerWrapper<TNotification> : NotificationHandlerWrapper
    where TNotification : INotification
{
    public override async Task Handle(INotification notification, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var typedNotification = (TNotification)notification;

        foreach (var handler in serviceProvider.GetServices<INotificationHandler<TNotification>>())
        {
            await handler.Handle(typedNotification, cancellationToken).ConfigureAwait(false);
        }
    }
}
