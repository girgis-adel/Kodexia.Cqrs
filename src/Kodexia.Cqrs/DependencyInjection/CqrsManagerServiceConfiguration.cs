using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Kodexia.Cqrs;

/// <summary>
/// Fluent configuration object for registering Kodexia.Cqrs services.
/// Pass an instance of this class to <see cref="CqrsManagerServiceCollectionExtensions.AddKodexiaCqrs(IServiceCollection, Action{CqrsManagerServiceConfiguration})"/>.
/// </summary>
public class CqrsManagerServiceConfiguration
{
    /// <summary>
    /// Gets or sets a predicate that filters which types are scanned during assembly registration.
    /// Only types for which the predicate returns <see langword="true"/> are registered.
    /// Defaults to accepting all types.
    /// </summary>
    public Func<Type, bool> TypeEvaluator { get; set; } = _ => true;

    /// <summary>
    /// Gets or sets the concrete type used as the <see cref="ICqrsManager"/> implementation.
    /// Defaults to <see cref="CqrsManager"/>.
    /// </summary>
    public Type MediatorImplementationType { get; set; } = typeof(CqrsManager);

    /// <summary>
    /// Gets or sets the notification publisher instance to use when <see cref="NotificationPublisherType"/> is not set.
    /// Defaults to <see cref="ForeachAwaitPublisher"/> (sequential execution).
    /// </summary>
    public INotificationPublisher NotificationPublisher { get; set; } = new ForeachAwaitPublisher();

    /// <summary>
    /// Gets or sets the type of the notification publisher to register with the DI container.
    /// When set, <see cref="NotificationPublisher"/> is ignored.
    /// </summary>
    public Type? NotificationPublisherType { get; set; }

    /// <summary>
    /// Gets or sets the DI <see cref="ServiceLifetime"/> for the <see cref="ICqrsManager"/>,
    /// <see cref="ISender"/>, and <see cref="IPublisher"/> registrations.
    /// Defaults to <see cref="ServiceLifetime.Transient"/>.
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;

    /// <summary>
    /// Gets or sets the strategy for ordering exception action processors relative to exception handlers.
    /// Defaults to <see cref="RequestExceptionActionProcessorStrategy.ApplyForUnhandledExceptions"/>.
    /// </summary>
    public RequestExceptionActionProcessorStrategy RequestExceptionActionProcessorStrategy { get; set; }
        = RequestExceptionActionProcessorStrategy.ApplyForUnhandledExceptions;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="IRequestPreProcessor{TRequest}"/> and
    /// <see cref="IRequestPostProcessor{TRequest, TResponse}"/> implementations are discovered
    /// and registered automatically during assembly scanning.
    /// Defaults to <see langword="false"/>; use explicit registration via
    /// <see cref="AddRequestPreProcessor{TImplementationType}"/> and
    /// <see cref="AddRequestPostProcessor{TImplementationType}"/> instead.
    /// </summary>
    public bool AutoRegisterRequestProcessors { get; set; }

    internal List<Assembly> AssembliesToRegister { get; } = [];

    /// <summary>Gets the list of pipeline behavior service descriptors to register.</summary>
    public List<ServiceDescriptor> BehaviorsToRegister { get; } = [];

    /// <summary>Gets the list of stream pipeline behavior service descriptors to register.</summary>
    public List<ServiceDescriptor> StreamBehaviorsToRegister { get; } = [];

    /// <summary>Gets the list of request pre-processor service descriptors to register.</summary>
    public List<ServiceDescriptor> RequestPreProcessorsToRegister { get; } = [];

    /// <summary>Gets the list of request post-processor service descriptors to register.</summary>
    public List<ServiceDescriptor> RequestPostProcessorsToRegister { get; } = [];

    // ─── Assembly Registration ───────────────────────────────────────────────

    /// <summary>Registers all handlers in the assembly containing <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">Any type in the target assembly.</typeparam>
    public CqrsManagerServiceConfiguration RegisterServicesFromAssemblyContaining<T>()
        => RegisterServicesFromAssemblyContaining(typeof(T));

    /// <summary>Registers all handlers in the assembly containing <paramref name="type"/>.</summary>
    public CqrsManagerServiceConfiguration RegisterServicesFromAssemblyContaining(Type type)
        => RegisterServicesFromAssembly(type.Assembly);

    /// <summary>Registers all handlers in the specified <paramref name="assembly"/>.</summary>
    public CqrsManagerServiceConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        AssembliesToRegister.Add(assembly);
        return this;
    }

    /// <summary>Registers all handlers in all specified <paramref name="assemblies"/>.</summary>
    public CqrsManagerServiceConfiguration RegisterServicesFromAssemblies(params Assembly[] assemblies)
    {
        AssembliesToRegister.AddRange(assemblies);
        return this;
    }

    // ─── Pipeline Behaviors ──────────────────────────────────────────────────

    /// <summary>Adds a closed pipeline behavior by service and implementation type.</summary>
    public CqrsManagerServiceConfiguration AddBehavior<TServiceType, TImplementationType>(
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddBehavior(typeof(TServiceType), typeof(TImplementationType), serviceLifetime);

    /// <summary>Adds a pipeline behavior by implementation type, inferring the service type from its implemented interfaces.</summary>
    public CqrsManagerServiceConfiguration AddBehavior<TImplementationType>(
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddBehavior(typeof(TImplementationType), serviceLifetime);

    /// <summary>Adds a pipeline behavior by implementation type, inferring the service type from its implemented interfaces.</summary>
    public CqrsManagerServiceConfiguration AddBehavior(
        Type implementationType,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        var interfaces = implementationType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            .ToList();

        if (interfaces.Count == 0)
            throw new InvalidOperationException(
                $"'{implementationType.Name}' does not implement {typeof(IPipelineBehavior<,>).FullName}.");

        foreach (var iface in interfaces)
            BehaviorsToRegister.Add(new ServiceDescriptor(iface, implementationType, serviceLifetime));

        return this;
    }

    /// <summary>Adds a closed pipeline behavior by explicit service and implementation types.</summary>
    public CqrsManagerServiceConfiguration AddBehavior(
        Type serviceType,
        Type implementationType,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        BehaviorsToRegister.Add(new ServiceDescriptor(serviceType, implementationType, serviceLifetime));
        return this;
    }

    /// <summary>
    /// Adds an open generic pipeline behavior that applies across all requests.
    /// </summary>
    /// <param name="openBehaviorType">
    /// An open generic type (e.g., <c>typeof(LoggingBehavior&lt;,&gt;)</c>) that implements
    /// <see cref="IPipelineBehavior{TRequest, TResponse}"/>.
    /// </param>
    /// <param name="serviceLifetime"></param>
    public CqrsManagerServiceConfiguration AddOpenBehavior(
        Type openBehaviorType,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        if (!openBehaviorType.IsGenericType)
            throw new InvalidOperationException($"'{openBehaviorType.Name}' must be a generic type.");

        var openInterfaces = openBehaviorType.GetInterfaces()
            .Where(i => i.IsGenericType)
            .Select(i => i.GetGenericTypeDefinition())
            .Where(i => i == typeof(IPipelineBehavior<,>))
            .ToHashSet();

        if (openInterfaces.Count == 0)
            throw new InvalidOperationException(
                $"'{openBehaviorType.Name}' must implement {typeof(IPipelineBehavior<,>).FullName}.");

        foreach (var iface in openInterfaces)
            BehaviorsToRegister.Add(new ServiceDescriptor(iface, openBehaviorType, serviceLifetime));

        return this;
    }

    /// <summary>Adds multiple open generic pipeline behaviors.</summary>
    public CqrsManagerServiceConfiguration AddOpenBehaviors(
        IEnumerable<Type> openBehaviorTypes,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        foreach (var type in openBehaviorTypes)
            AddOpenBehavior(type, serviceLifetime);
        return this;
    }

    /// <summary>Adds multiple open generic pipeline behaviors with individual lifetime settings.</summary>
    public CqrsManagerServiceConfiguration AddOpenBehaviors(IEnumerable<OpenBehavior> openBehaviors)
    {
        foreach (var b in openBehaviors)
            AddOpenBehavior(b.OpenBehaviorType, b.ServiceLifetime);
        return this;
    }

    // ─── Stream Behaviors ────────────────────────────────────────────────────

    /// <summary>Adds a stream pipeline behavior by service and implementation types.</summary>
    public CqrsManagerServiceConfiguration AddStreamBehavior<TServiceType, TImplementationType>(
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddStreamBehavior(typeof(TServiceType), typeof(TImplementationType), serviceLifetime);

    /// <summary>Adds a stream pipeline behavior by explicit service and implementation types.</summary>
    public CqrsManagerServiceConfiguration AddStreamBehavior(
        Type serviceType,
        Type implementationType,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        StreamBehaviorsToRegister.Add(new ServiceDescriptor(serviceType, implementationType, serviceLifetime));
        return this;
    }

    /// <summary>Adds a stream pipeline behavior by implementation type, inferring the service type.</summary>
    public CqrsManagerServiceConfiguration AddStreamBehavior<TImplementationType>(
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddStreamBehavior(typeof(TImplementationType), serviceLifetime);

    /// <summary>Adds a stream pipeline behavior by implementation type, inferring the service type.</summary>
    public CqrsManagerServiceConfiguration AddStreamBehavior(
        Type implementationType,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        var interfaces = implementationType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamPipelineBehavior<,>))
            .ToList();

        if (interfaces.Count == 0)
            throw new InvalidOperationException(
                $"'{implementationType.Name}' does not implement {typeof(IStreamPipelineBehavior<,>).FullName}.");

        foreach (var iface in interfaces)
            StreamBehaviorsToRegister.Add(new ServiceDescriptor(iface, implementationType, serviceLifetime));

        return this;
    }

    /// <summary>Adds an open generic stream pipeline behavior.</summary>
    public CqrsManagerServiceConfiguration AddOpenStreamBehavior(
        Type openBehaviorType,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        if (!openBehaviorType.IsGenericType)
            throw new InvalidOperationException($"'{openBehaviorType.Name}' must be a generic type.");

        var openInterfaces = openBehaviorType.GetInterfaces()
            .Where(i => i.IsGenericType)
            .Select(i => i.GetGenericTypeDefinition())
            .Where(i => i == typeof(IStreamPipelineBehavior<,>))
            .ToHashSet();

        if (openInterfaces.Count == 0)
            throw new InvalidOperationException(
                $"'{openBehaviorType.Name}' must implement {typeof(IStreamPipelineBehavior<,>).FullName}.");

        foreach (var iface in openInterfaces)
            StreamBehaviorsToRegister.Add(new ServiceDescriptor(iface, openBehaviorType, serviceLifetime));

        return this;
    }

    // ─── Pre/Post Processors ─────────────────────────────────────────────────

    /// <summary>Adds a request pre-processor by service and implementation types.</summary>
    public CqrsManagerServiceConfiguration AddRequestPreProcessor<TServiceType, TImplementationType>(
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddRequestPreProcessor(typeof(TServiceType), typeof(TImplementationType), serviceLifetime);

    /// <summary>Adds a request pre-processor by explicit service and implementation types.</summary>
    public CqrsManagerServiceConfiguration AddRequestPreProcessor(
        Type serviceType,
        Type implementationType,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        RequestPreProcessorsToRegister.Add(new ServiceDescriptor(serviceType, implementationType, serviceLifetime));
        return this;
    }

    /// <summary>Adds a request pre-processor by implementation type, inferring the service type.</summary>
    public CqrsManagerServiceConfiguration AddRequestPreProcessor<TImplementationType>(
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddRequestPreProcessor(typeof(TImplementationType), serviceLifetime);

    /// <summary>Adds a request pre-processor by implementation type, inferring the service type.</summary>
    public CqrsManagerServiceConfiguration AddRequestPreProcessor(
        Type implementationType,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        var interfaces = implementationType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestPreProcessor<>))
            .ToList();

        if (interfaces.Count == 0)
            throw new InvalidOperationException(
                $"'{implementationType.Name}' does not implement {typeof(IRequestPreProcessor<>).FullName}.");

        foreach (var iface in interfaces)
            RequestPreProcessorsToRegister.Add(new ServiceDescriptor(iface, implementationType, serviceLifetime));

        return this;
    }

    /// <summary>Adds an open generic request pre-processor.</summary>
    public CqrsManagerServiceConfiguration AddOpenRequestPreProcessor(
        Type openBehaviorType,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        if (!openBehaviorType.IsGenericType)
            throw new InvalidOperationException($"'{openBehaviorType.Name}' must be a generic type.");

        var openInterfaces = openBehaviorType.GetInterfaces()
            .Where(i => i.IsGenericType)
            .Select(i => i.GetGenericTypeDefinition())
            .Where(i => i == typeof(IRequestPreProcessor<>))
            .ToHashSet();

        if (openInterfaces.Count == 0)
            throw new InvalidOperationException(
                $"'{openBehaviorType.Name}' must implement {typeof(IRequestPreProcessor<>).FullName}.");

        foreach (var iface in openInterfaces)
            RequestPreProcessorsToRegister.Add(new ServiceDescriptor(iface, openBehaviorType, serviceLifetime));

        return this;
    }

    /// <summary>Adds a request post-processor by service and implementation types.</summary>
    public CqrsManagerServiceConfiguration AddRequestPostProcessor<TServiceType, TImplementationType>(
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddRequestPostProcessor(typeof(TServiceType), typeof(TImplementationType), serviceLifetime);

    /// <summary>Adds a request post-processor by explicit service and implementation types.</summary>
    public CqrsManagerServiceConfiguration AddRequestPostProcessor(
        Type serviceType,
        Type implementationType,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        RequestPostProcessorsToRegister.Add(new ServiceDescriptor(serviceType, implementationType, serviceLifetime));
        return this;
    }

    /// <summary>Adds a request post-processor by implementation type, inferring the service type.</summary>
    public CqrsManagerServiceConfiguration AddRequestPostProcessor<TImplementationType>(
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddRequestPostProcessor(typeof(TImplementationType), serviceLifetime);

    /// <summary>Adds a request post-processor by implementation type, inferring the service type.</summary>
    public CqrsManagerServiceConfiguration AddRequestPostProcessor(
        Type implementationType,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        var interfaces = implementationType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestPostProcessor<,>))
            .ToList();

        if (interfaces.Count == 0)
            throw new InvalidOperationException(
                $"'{implementationType.Name}' does not implement {typeof(IRequestPostProcessor<,>).FullName}.");

        foreach (var iface in interfaces)
            RequestPostProcessorsToRegister.Add(new ServiceDescriptor(iface, implementationType, serviceLifetime));

        return this;
    }

    /// <summary>Adds an open generic request post-processor.</summary>
    public CqrsManagerServiceConfiguration AddOpenRequestPostProcessor(
        Type openBehaviorType,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        if (!openBehaviorType.IsGenericType)
            throw new InvalidOperationException($"'{openBehaviorType.Name}' must be a generic type.");

        var openInterfaces = openBehaviorType.GetInterfaces()
            .Where(i => i.IsGenericType)
            .Select(i => i.GetGenericTypeDefinition())
            .Where(i => i == typeof(IRequestPostProcessor<,>))
            .ToHashSet();

        if (openInterfaces.Count == 0)
            throw new InvalidOperationException(
                $"'{openBehaviorType.Name}' must implement {typeof(IRequestPostProcessor<,>).FullName}.");

        foreach (var iface in openInterfaces)
            RequestPostProcessorsToRegister.Add(new ServiceDescriptor(iface, openBehaviorType, serviceLifetime));

        return this;
    }
}
