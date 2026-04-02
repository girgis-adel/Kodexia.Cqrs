using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kodexia.Cqrs.Internal;

/// <summary>
/// Internal helper that performs assembly scanning and Hub service registration.
/// </summary>
internal static class ServiceRegistrar
{
    public static void AddHubClasses(IServiceCollection services, HubServiceConfiguration configuration)
    {
        var assembliesToScan = configuration.AssembliesToRegister.Distinct().ToArray();

        var singleOpenInterfaces = new[]
        {
            typeof(IConsumer<,>),
            typeof(IConsumer<>),
            typeof(IProvider<,>)
        };

        var multiOpenInterfaces = new List<Type>
        {
            typeof(ISubscriber<>),
            typeof(IFallbackHandler<,,>),
            typeof(IExceptionObserver<,>)
        };

        if (configuration.AutoRegisterMessageProcessors)
        {
            multiOpenInterfaces.Add(typeof(IPreConsumer<>));
            multiOpenInterfaces.Add(typeof(IPostConsumer<,>));
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

    public static void AddRequiredServices(IServiceCollection services, HubServiceConfiguration cfg)
    {
        services.TryAdd(new ServiceDescriptor(typeof(IHubExchange), cfg.HubImplementationType, cfg.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(IDeliveryAgent), sp => sp.GetRequiredService<IHubExchange>(), cfg.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(IBroadcastAgent), sp => sp.GetRequiredService<IHubExchange>(), cfg.Lifetime));

        var strategyDescriptor = cfg.BroadcastStrategyType is not null
            ? new ServiceDescriptor(typeof(IBroadcastStrategy), cfg.BroadcastStrategyType, cfg.Lifetime)
            : new ServiceDescriptor(typeof(IBroadcastStrategy), cfg.BroadcastStrategy);

        services.TryAdd(strategyDescriptor);

        // Exception Handling Order
        if (cfg.ExceptionObservationStrategy == ExceptionObservationStrategy.ApplyForUnhandledExceptions)
        {
            RegisterInterceptorIfExist(services, typeof(ExceptionObservationInterceptor<,>), typeof(IExceptionObserver<,>));
            RegisterInterceptorIfExist(services, typeof(FallbackInterceptor<,>), typeof(IFallbackHandler<,,>));
        }
        else
        {
            RegisterInterceptorIfExist(services, typeof(FallbackInterceptor<,>), typeof(IFallbackHandler<,,>));
            RegisterInterceptorIfExist(services, typeof(ExceptionObservationInterceptor<,>), typeof(IExceptionObserver<,>));
        }

        // Pre/Post Processors
        if (cfg.PreConsumersToRegister.Count > 0)
        {
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IInterceptor<,>), typeof(PreConsumptionInterceptor<,>), ServiceLifetime.Transient));
            services.TryAddEnumerable(cfg.PreConsumersToRegister);
        }

        if (cfg.PostConsumersToRegister.Count > 0)
        {
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IInterceptor<,>), typeof(PostConsumptionInterceptor<,>), ServiceLifetime.Transient));
            services.TryAddEnumerable(cfg.PostConsumersToRegister);
        }

        foreach (var descriptor in cfg.InterceptorsToRegister)
            services.TryAddEnumerable(descriptor);

        foreach (var descriptor in cfg.StreamInterceptorsToRegister)
            services.TryAddEnumerable(descriptor);
    }

    private static void RegisterInterceptorIfExist(
        IServiceCollection services,
        Type interceptorType,
        Type subHandlerType)
    {
        var hasAny = services
            .Where(s => !s.IsKeyedService)
            .Select(s => s.ImplementationType)
            .OfType<Type>()
            .SelectMany(t => t.GetInterfaces())
            .Where(t => t.IsGenericType)
            .Select(t => t.GetGenericTypeDefinition())
            .Any(t => t == subHandlerType);

        if (hasAny)
        {
            services.TryAddEnumerable(new ServiceDescriptor(
                typeof(IInterceptor<,>), interceptorType, ServiceLifetime.Transient));
        }
    }

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
