using Microsoft.Extensions.DependencyInjection;

namespace Herald.Internal;

internal static class Pipeline
{
    public static Task<TResponse> Execute<TRequest, TResponse>(
        TRequest request,
        IServiceProvider serviceProvider,
        RequestHandlerDelegate<TResponse> handler,
        CancellationToken cancellationToken)
        where TRequest : notnull
    {
        var services = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>();
        var behaviors = services as IPipelineBehavior<TRequest, TResponse>[] ?? services.ToArray();

        var next = handler;

        // Wrap from the inside out so the first registered behavior is the outermost.
        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var current = next;
            next = () => behavior.Handle(request, current, cancellationToken);
        }

        return next();
    }
}
