using System.Reflection;

namespace Herald;

/// <summary>
/// Configures which assemblies Herald scans for handlers and which pipeline behaviors it registers.
/// </summary>
public sealed class HeraldConfiguration
{
    internal List<Assembly> Assemblies { get; } = [];

    internal List<(Type ServiceType, Type ImplementationType)> Behaviors { get; } = [];

    /// <summary>
    /// Scans the assembly for request and notification handlers.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The configuration, for chaining.</returns>
    public HeraldConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (!Assemblies.Contains(assembly))
        {
            Assemblies.Add(assembly);
        }

        return this;
    }

    /// <summary>
    /// Scans the assemblies for request and notification handlers.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The configuration, for chaining.</returns>
    public HeraldConfiguration RegisterServicesFromAssemblies(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (var assembly in assemblies)
        {
            RegisterServicesFromAssembly(assembly);
        }

        return this;
    }

    /// <summary>
    /// Scans the assembly containing <typeparamref name="T"/> for request and notification handlers.
    /// </summary>
    /// <typeparam name="T">A type in the assembly to scan.</typeparam>
    /// <returns>The configuration, for chaining.</returns>
    public HeraldConfiguration RegisterServicesFromAssemblyContaining<T>()
        => RegisterServicesFromAssembly(typeof(T).Assembly);

    /// <summary>
    /// Adds an open generic pipeline behavior, such as <c>typeof(LoggingBehavior&lt;,&gt;)</c>, that applies to every request
    /// whose types satisfy the behavior's generic constraints.
    /// </summary>
    /// <remarks>Behaviors run in the order they are added: the first added behavior is the outermost.</remarks>
    /// <param name="openBehaviorType">An open generic type implementing <see cref="IPipelineBehavior{TRequest,TResponse}"/>.</param>
    /// <returns>The configuration, for chaining.</returns>
    /// <exception cref="ArgumentException">The type is not an open generic pipeline behavior.</exception>
    public HeraldConfiguration AddOpenBehavior(Type openBehaviorType)
    {
        ArgumentNullException.ThrowIfNull(openBehaviorType);

        var implementsBehavior = openBehaviorType.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

        if (!openBehaviorType.IsGenericTypeDefinition || openBehaviorType.GetGenericArguments().Length != 2 || !implementsBehavior)
        {
            throw new ArgumentException(
                $"{openBehaviorType.Name} must be an open generic type with two type parameters implementing {typeof(IPipelineBehavior<,>).Name}.",
                nameof(openBehaviorType));
        }

        Behaviors.Add((typeof(IPipelineBehavior<,>), openBehaviorType));
        return this;
    }

    /// <summary>
    /// Adds a closed pipeline behavior that applies to the specific request type(s) it implements
    /// <see cref="IPipelineBehavior{TRequest,TResponse}"/> for.
    /// </summary>
    /// <remarks>Behaviors run in the order they are added: the first added behavior is the outermost.</remarks>
    /// <typeparam name="TBehavior">A closed type implementing <see cref="IPipelineBehavior{TRequest,TResponse}"/>.</typeparam>
    /// <returns>The configuration, for chaining.</returns>
    /// <exception cref="ArgumentException">The type does not implement a closed pipeline behavior.</exception>
    public HeraldConfiguration AddBehavior<TBehavior>()
        where TBehavior : class
    {
        var behaviorInterfaces = typeof(TBehavior).GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            .ToList();

        if (behaviorInterfaces.Count == 0)
        {
            throw new ArgumentException(
                $"{typeof(TBehavior).Name} must implement {typeof(IPipelineBehavior<,>).Name}.",
                nameof(TBehavior));
        }

        foreach (var behaviorInterface in behaviorInterfaces)
        {
            Behaviors.Add((behaviorInterface, typeof(TBehavior)));
        }

        return this;
    }
}
