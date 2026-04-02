using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Kodexia.Cqrs;

/// <summary>
/// Fluent configuration object for registering Kodexia Hub services.
/// </summary>
public class HubServiceConfiguration
{
    /// <summary>
    /// Gets or sets a predicate that filters which types are scanned during assembly registration.
    /// </summary>
    public Func<Type, bool> TypeEvaluator { get; set; } = _ => true;

    /// <summary>
    /// Gets or sets the concrete type used as the <see cref="IHubExchange"/> implementation.
    /// Defaults to <see cref="HubExchange"/>.
    /// </summary>
    public Type HubImplementationType { get; set; } = typeof(HubExchange);

    /// <summary>
    /// Gets or sets the broadcast strategy instance.
    /// Defaults to <see cref="ForeachAwaitBroadcastStrategy"/>.
    /// </summary>
    public IBroadcastStrategy BroadcastStrategy { get; set; } = new ForeachAwaitBroadcastStrategy();

    /// <summary>
    /// Gets or sets the type of the broadcast strategy to register.
    /// When set, <see cref="BroadcastStrategy"/> is ignored.
    /// </summary>
    public Type? BroadcastStrategyType { get; set; }

    /// <summary>
    /// Gets or sets the DI <see cref="ServiceLifetime"/> for the Hub registrations.
    /// Defaults to <see cref="ServiceLifetime.Transient"/>.
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;

    /// <summary>
    /// Gets or sets the strategy for ordering exception observation interceptors.
    /// </summary>
    public ExceptionObservationStrategy ExceptionObservationStrategy { get; set; }
        = ExceptionObservationStrategy.ApplyForUnhandledExceptions;

    /// <summary>
    /// Gets or sets a value indicating whether pre and post consumers are automatically registered during assembly scanning.
    /// Defaults to false.
    /// </summary>
    public bool AutoRegisterMessageProcessors { get; set; }

    internal List<Assembly> AssembliesToRegister { get; } = [];
    public List<ServiceDescriptor> InterceptorsToRegister { get; } = [];
    public List<ServiceDescriptor> StreamInterceptorsToRegister { get; } = [];
    public List<ServiceDescriptor> PreConsumersToRegister { get; } = [];
    public List<ServiceDescriptor> PostConsumersToRegister { get; } = [];

    // --- Assembly Registration ---

    public HubServiceConfiguration RegisterHubClassesFromAssemblyContaining<T>()
        => RegisterHubClassesFromAssemblyContaining(typeof(T));

    public HubServiceConfiguration RegisterHubClassesFromAssemblyContaining(Type type)
        => RegisterHubClassesFromAssembly(type.Assembly);

    public HubServiceConfiguration RegisterHubClassesFromAssembly(Assembly assembly)
    {
        AssembliesToRegister.Add(assembly);
        return this;
    }

    public HubServiceConfiguration RegisterHubClassesFromAssemblies(params Assembly[] assemblies)
    {
        AssembliesToRegister.AddRange(assemblies);
        return this;
    }

    // --- Interceptors ---

    public HubServiceConfiguration AddInterceptor<TImplementationType>(
        ServiceLifetime lifetime = ServiceLifetime.Transient)
        => AddInterceptor(typeof(TImplementationType), lifetime);

    public HubServiceConfiguration AddInterceptor(
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        InterceptorsToRegister.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
        return this;
    }

    public HubServiceConfiguration AddInterceptor(
        Type implementationType,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var interfaces = implementationType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IInterceptor<,>))
            .ToList();

        if (interfaces.Count == 0)
            throw new InvalidOperationException($"'{implementationType.Name}' must implement {typeof(IInterceptor<,>).FullName}.");

        foreach (var iface in interfaces)
            InterceptorsToRegister.Add(new ServiceDescriptor(iface, implementationType, lifetime));

        return this;
    }

    public HubServiceConfiguration AddOpenInterceptor(
        Type openInterceptorType,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        if (!openInterceptorType.IsGenericType)
            throw new InvalidOperationException($"'{openInterceptorType.Name}' must be generic.");

        var openInterfaces = openInterceptorType.GetInterfaces()
            .Where(i => i.IsGenericType)
            .Select(i => i.GetGenericTypeDefinition())
            .Where(i => i == typeof(IInterceptor<,>))
            .ToHashSet();

        if (openInterfaces.Count == 0)
            throw new InvalidOperationException($"'{openInterceptorType.Name}' must implement {typeof(IInterceptor<,>).FullName}.");

        foreach (var iface in openInterfaces)
            InterceptorsToRegister.Add(new ServiceDescriptor(iface, openInterceptorType, lifetime));

        return this;
    }

    public HubServiceConfiguration AddOpenInterceptors(
        IEnumerable<Type> openInterceptorTypes,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        foreach (var type in openInterceptorTypes)
            AddOpenInterceptor(type, lifetime);
        return this;
    }

    public HubServiceConfiguration AddOpenInterceptors(IEnumerable<OpenInterceptor> openInterceptors)
    {
        foreach (var i in openInterceptors)
            AddOpenInterceptor(i.OpenInterceptorType, i.ServiceLifetime);
        return this;
    }

    // --- Stream Interceptors ---

    public HubServiceConfiguration AddStreamInterceptor<TImplementationType>(
        ServiceLifetime lifetime = ServiceLifetime.Transient)
        => AddStreamInterceptor(typeof(TImplementationType), lifetime);

    public HubServiceConfiguration AddStreamInterceptor(
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        StreamInterceptorsToRegister.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
        return this;
    }

    public HubServiceConfiguration AddStreamInterceptor(
        Type implementationType,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var interfaces = implementationType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamInterceptor<,>))
            .ToList();

        if (interfaces.Count == 0)
            throw new InvalidOperationException($"'{implementationType.Name}' must implement {typeof(IStreamInterceptor<,>).FullName}.");

        foreach (var iface in interfaces)
            StreamInterceptorsToRegister.Add(new ServiceDescriptor(iface, implementationType, lifetime));

        return this;
    }

    public HubServiceConfiguration AddOpenStreamInterceptor(
        Type openInterceptorType,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        if (!openInterceptorType.IsGenericType)
            throw new InvalidOperationException($"'{openInterceptorType.Name}' must be generic.");

        var openInterfaces = openInterceptorType.GetInterfaces()
            .Where(i => i.IsGenericType)
            .Select(i => i.GetGenericTypeDefinition())
            .Where(i => i == typeof(IStreamInterceptor<,>))
            .ToHashSet();

        if (openInterfaces.Count == 0)
            throw new InvalidOperationException($"'{openInterceptorType.Name}' must implement {typeof(IStreamInterceptor<,>).FullName}.");

        foreach (var iface in openInterfaces)
            StreamInterceptorsToRegister.Add(new ServiceDescriptor(iface, openInterceptorType, lifetime));

        return this;
    }

    // --- Pre/Post Consumers ---

    public HubServiceConfiguration AddPreConsumer<TImplementationType>(
        ServiceLifetime lifetime = ServiceLifetime.Transient)
        => AddPreConsumer(typeof(TImplementationType), lifetime);

    public HubServiceConfiguration AddPreConsumer(
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        PreConsumersToRegister.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
        return this;
    }

    public HubServiceConfiguration AddPreConsumer(
        Type implementationType,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var interfaces = implementationType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPreConsumer<>))
            .ToList();

        if (interfaces.Count == 0)
            throw new InvalidOperationException($"'{implementationType.Name}' must implement {typeof(IPreConsumer<>).FullName}.");

        foreach (var iface in interfaces)
        {
            var serviceType = implementationType.IsGenericTypeDefinition ? iface.GetGenericTypeDefinition() : iface;
            PreConsumersToRegister.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
        }

        return this;
    }

    public HubServiceConfiguration AddPostConsumer<TImplementationType>(
        ServiceLifetime lifetime = ServiceLifetime.Transient)
        => AddPostConsumer(typeof(TImplementationType), lifetime);

    public HubServiceConfiguration AddPostConsumer(
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        PostConsumersToRegister.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
        return this;
    }

    public HubServiceConfiguration AddPostConsumer(
        Type implementationType,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var interfaces = implementationType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPostConsumer<,>))
            .ToList();

        if (interfaces.Count == 0)
            throw new InvalidOperationException($"'{implementationType.Name}' must implement {typeof(IPostConsumer<,>).FullName}.");

        foreach (var iface in interfaces)
        {
            var serviceType = implementationType.IsGenericTypeDefinition ? iface.GetGenericTypeDefinition() : iface;
            PostConsumersToRegister.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
        }

        return this;
    }
}
