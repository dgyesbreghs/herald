using System.Collections.Concurrent;
using Herald.Internal;

namespace Herald;

/// <summary>
/// The default <see cref="IMediator"/> implementation, resolving handlers and behaviors from the <see cref="IServiceProvider"/>.
/// </summary>
public sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private static readonly ConcurrentDictionary<Type, RequestHandlerWrapper> RequestHandlers = new();
    private static readonly ConcurrentDictionary<Type, VoidRequestHandlerWrapper> VoidRequestHandlers = new();
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> NotificationHandlers = new();

    /// <inheritdoc />
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = (RequestHandlerWrapper<TResponse>)RequestHandlers.GetOrAdd(
            request.GetType(),
            static requestType => (RequestHandlerWrapper)Activator.CreateInstance(
                typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, typeof(TResponse)))!);

        return wrapper.Handle(request, serviceProvider, cancellationToken);
    }

    /// <inheritdoc />
    public Task Send(IRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = VoidRequestHandlers.GetOrAdd(
            request.GetType(),
            static requestType => (VoidRequestHandlerWrapper)Activator.CreateInstance(
                typeof(VoidRequestHandlerWrapper<>).MakeGenericType(requestType))!);

        return wrapper.Handle(request, serviceProvider, cancellationToken);
    }

    /// <inheritdoc />
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var wrapper = NotificationHandlers.GetOrAdd(
            notification.GetType(),
            static notificationType => (NotificationHandlerWrapper)Activator.CreateInstance(
                typeof(NotificationHandlerWrapper<>).MakeGenericType(notificationType))!);

        return wrapper.Handle(notification, serviceProvider, cancellationToken);
    }
}
