using Microsoft.Extensions.DependencyInjection;

namespace Herald.Internal;

internal abstract class RequestHandlerWrapper;

internal abstract class RequestHandlerWrapper<TResponse> : RequestHandlerWrapper
{
    public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

internal sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    public override Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

        return Pipeline.Execute(
            typedRequest,
            serviceProvider,
            () => handler.Handle(typedRequest, cancellationToken),
            cancellationToken);
    }
}

internal abstract class VoidRequestHandlerWrapper
{
    public abstract Task Handle(IRequest request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

internal sealed class VoidRequestHandlerWrapper<TRequest> : VoidRequestHandlerWrapper
    where TRequest : IRequest
{
    public override Task Handle(IRequest request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest>>();

        return Pipeline.Execute(
            typedRequest,
            serviceProvider,
            async () =>
            {
                await handler.Handle(typedRequest, cancellationToken).ConfigureAwait(false);
                return Unit.Value;
            },
            cancellationToken);
    }
}
