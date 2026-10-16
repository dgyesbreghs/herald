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

        // Resolve the handler only when the pipeline reaches it, so a short-circuiting behavior never constructs it.
        return Pipeline.Execute(
            typedRequest,
            serviceProvider,
            () => serviceProvider
                .GetRequiredService<IRequestHandler<TRequest, TResponse>>()
                .Handle(typedRequest, cancellationToken),
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

        // Resolve the handler only when the pipeline reaches it, so a short-circuiting behavior never constructs it.
        return Pipeline.Execute(
            typedRequest,
            serviceProvider,
            async () =>
            {
                var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest>>();
                await handler.Handle(typedRequest, cancellationToken).ConfigureAwait(false);
                return Unit.Value;
            },
            cancellationToken);
    }
}
