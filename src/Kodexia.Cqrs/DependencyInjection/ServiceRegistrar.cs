using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kodexia.Cqrs.Internal;

/// <summary>
/// Internal helper that performs assembly scanning and required service registration.
/// Not part of the public API — subject to change without notice.
/// </summary>
internal static class ServiceRegistrar
{
    public static void AddCqrsClasses(IServiceCollection services, CqrsManagerServiceConfiguration configuration)
    {
        var assembliesToScan = configuration.AssembliesToRegister.Distinct().ToArray();

        var singleOpenInterfaces = new[]
        {
            typeof(IRequestHandler<,>),
            typeof(IRequestHandler<>),
            typeof(IStreamRequestHandler<,>)
        };

        var multiOpenInterfaces = new List<Type>
        {
            typeof(INotificationHandler<>),
            typeof(IRequestExceptionHandler<,,>),
            typeof(IRequestExceptionAction<,>)
        };

        if (configuration.AutoRegisterRequestProcessors)
        {
            multiOpenInterfaces.Add(typeof(IRequestPreProcessor<>));
            multiOpenInterfaces.Add(typeof(IRequestPostProcessor<,>));
        }

        var allOpenInterfaces = singleOpenInterfaces.Concat(multiOpenInterfaces).ToHashSet();

        var concretions = assembliesToScan
            .SelectMany(GetLoadableTypes)
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(configuration.TypeEvaluator)
            .ToList();

        foreach (var type in concretions)
        {
            foreach (var @interface in type.GetInterfaces())
            {
                if (!@interface.IsGenericType)
                    continue;

                var openInterface = @interface.GetGenericTypeDefinition();
                if (!allOpenInterfaces.Contains(openInterface))
                    continue;

                if (type.IsGenericTypeDefinition)
                {
                    if (singleOpenInterfaces.Contains(openInterface))
                        services.TryAddTransient(openInterface, type);
                    else
                        services.AddTransient(openInterface, type);
                }
                else
                {
                    if (singleOpenInterfaces.Contains(openInterface))
                        services.TryAddTransient(@interface, type);
                    else
                        services.AddTransient(@interface, type);
                }
            }
        }
    }

    public static void AddRequiredServices(IServiceCollection services, CqrsManagerServiceConfiguration cfg)
    {
        services.TryAdd(new ServiceDescriptor(typeof(ICqrsManager), cfg.CqrsManagerImplementationType, cfg.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(ISender), sp => sp.GetRequiredService<ICqrsManager>(), cfg.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(IPublisher), sp => sp.GetRequiredService<ICqrsManager>(), cfg.Lifetime));

        var publisherDescriptor = cfg.NotificationPublisherType is not null
            ? new ServiceDescriptor(typeof(INotificationPublisher), cfg.NotificationPublisherType, cfg.Lifetime)
            : new ServiceDescriptor(typeof(INotificationPublisher), cfg.NotificationPublisher);

        services.TryAdd(publisherDescriptor);

        // Registration order determines pipeline order:
        //   ApplyForUnhandledExceptions (default): actions → handlers (actions only fire if handler did not suppress)
        //   ApplyForAllExceptions:                 handlers → actions (actions always fire)
        if (cfg.RequestExceptionActionProcessorStrategy == RequestExceptionActionProcessorStrategy.ApplyForUnhandledExceptions)
        {
            RegisterBehaviorIfImplementationsExist(services, typeof(RequestExceptionActionProcessorBehavior<,>), typeof(IRequestExceptionAction<,>));
            RegisterBehaviorIfImplementationsExist(services, typeof(RequestExceptionProcessorBehavior<,>), typeof(IRequestExceptionHandler<,,>));
        }
        else
        {
            RegisterBehaviorIfImplementationsExist(services, typeof(RequestExceptionProcessorBehavior<,>), typeof(IRequestExceptionHandler<,,>));
            RegisterBehaviorIfImplementationsExist(services, typeof(RequestExceptionActionProcessorBehavior<,>), typeof(IRequestExceptionAction<,>));
        }

        if (cfg.RequestPreProcessorsToRegister.Count > 0)
        {
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestPreProcessorBehavior<,>), ServiceLifetime.Transient));
            services.TryAddEnumerable(cfg.RequestPreProcessorsToRegister);
        }

        if (cfg.RequestPostProcessorsToRegister.Count > 0)
        {
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestPostProcessorBehavior<,>), ServiceLifetime.Transient));
            services.TryAddEnumerable(cfg.RequestPostProcessorsToRegister);
        }

        foreach (var descriptor in cfg.BehaviorsToRegister)
            services.TryAddEnumerable(descriptor);

        foreach (var descriptor in cfg.StreamBehaviorsToRegister)
            services.TryAddEnumerable(descriptor);
    }

    private static void RegisterBehaviorIfImplementationsExist(
        IServiceCollection services,
        Type behaviorType,
        Type subBehaviorType)
    {
        var hasAny = services
            .Where(s => !s.IsKeyedService)
            .Select(s => s.ImplementationType)
            .OfType<Type>()
            .SelectMany(t => t.GetInterfaces())
            .Where(t => t.IsGenericType)
            .Select(t => t.GetGenericTypeDefinition())
            .Any(t => t == subBehaviorType);

        if (hasAny)
        {
            services.TryAddEnumerable(new ServiceDescriptor(
                typeof(IPipelineBehavior<,>), behaviorType, ServiceLifetime.Transient));
        }
    }

    /// <summary>
    /// Safely loads types from an assembly, swallowing <see cref="ReflectionTypeLoadException"/>
    /// for types that cannot be loaded (e.g. missing transitive dependencies).
    /// </summary>
    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
