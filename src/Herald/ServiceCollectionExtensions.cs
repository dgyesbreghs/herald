using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Herald;

/// <summary>
/// Registration of the <see cref="IMediator"/> and its handlers.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="IMediator"/> and every <see cref="IRequestHandler{TRequest,TResponse}"/>
    /// found in the assembly containing <typeparamref name="T"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <typeparam name="T">A type in the assembly to scan.</typeparam>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddHeraldFromAssemblyContaining<T>(this IServiceCollection services)
        => services.AddHerald(typeof(T).Assembly);

    /// <summary>
    /// Registers the <see cref="IMediator"/> and every <see cref="IRequestHandler{TRequest,TResponse}"/>
    /// found in the given assemblies.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddHerald(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.TryAddTransient<IMediator, Mediator>();

        var handlerTypes = assemblies
            .SelectMany(assembly => assembly.DefinedTypes)
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false });

        foreach (var handlerType in handlerTypes)
        {
            var handlerInterfaces = handlerType.ImplementedInterfaces
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

            foreach (var handlerInterface in handlerInterfaces)
            {
                services.TryAddTransient(handlerInterface, handlerType);
            }
        }

        return services;
    }
}
