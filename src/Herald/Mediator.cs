using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Herald;

/// <summary>
/// The default <see cref="IMediator"/> implementation, resolving handlers from the <see cref="IServiceProvider"/>.
/// </summary>
public sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private static readonly ConcurrentDictionary<Type, RequestHandlerWrapper> Wrappers = new();

    /// <inheritdoc />
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = (RequestHandlerWrapper<TResponse>)Wrappers.GetOrAdd(
            request.GetType(),
            static requestType => (RequestHandlerWrapper)Activator.CreateInstance(
                typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, typeof(TResponse)))!);

        return wrapper.Handle(request, serviceProvider, cancellationToken);
    }

    private abstract class RequestHandlerWrapper;

    private abstract class RequestHandlerWrapper<TResponse> : RequestHandlerWrapper
    {
        public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    private sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public override Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
            => serviceProvider
                .GetRequiredService<IRequestHandler<TRequest, TResponse>>()
                .Handle((TRequest)request, cancellationToken);
    }
}
