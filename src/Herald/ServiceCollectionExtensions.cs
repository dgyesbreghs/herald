using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Herald;

/// <summary>
/// Registration of the <see cref="IMediator"/>, its handlers and pipeline behaviors.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="IMediator"/> and the handlers and behaviors described by <paramref name="configure"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">Configures the assemblies to scan and the pipeline behaviors.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddHerald(this IServiceCollection services, Action<HeraldConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var configuration = new HeraldConfiguration();
        configure(configuration);

        services.TryAddTransient<IMediator, Mediator>();
        services.TryAddTransient<ISender>(sp => sp.GetRequiredService<IMediator>());
        services.TryAddTransient<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        foreach (var assembly in configuration.Assemblies)
        {
            RegisterHandlers(services, assembly);
        }

        foreach (var (serviceType, implementationType) in configuration.Behaviors)
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient(serviceType, implementationType));
        }

        return services;
    }

    /// <summary>
    /// Registers the <see cref="IMediator"/> and every handler found in the given assemblies.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddHerald(this IServiceCollection services, params Assembly[] assemblies)
        => services.AddHerald(configuration => configuration.RegisterServicesFromAssemblies(assemblies));

    /// <summary>
    /// Registers the <see cref="IMediator"/> and every handler found in the assembly containing <typeparamref name="T"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <typeparam name="T">A type in the assembly to scan.</typeparam>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddHeraldFromAssemblyContaining<T>(this IServiceCollection services)
        => services.AddHerald(configuration => configuration.RegisterServicesFromAssemblyContaining<T>());

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.DefinedTypes
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false });

        foreach (var handlerType in handlerTypes)
        {
            foreach (var handlerInterface in handlerType.ImplementedInterfaces.Where(i => i.IsGenericType))
            {
                var definition = handlerInterface.GetGenericTypeDefinition();

                if (definition == typeof(IRequestHandler<,>) || definition == typeof(IRequestHandler<>))
                {
                    // One handler per request type: the first registration wins.
                    services.TryAddTransient(handlerInterface, handlerType);
                }
                else if (definition == typeof(INotificationHandler<>))
                {
                    // Any number of handlers per notification type, each implementation registered once.
                    services.TryAddEnumerable(ServiceDescriptor.Transient(handlerInterface, handlerType));
                }
            }
        }
    }
}
