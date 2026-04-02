using Kodexia.Cqrs.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Kodexia.Cqrs;

/// <summary>
/// Provides extension methods for registering Kodexia Hub services.
/// </summary>
public static class HubServiceCollectionExtensions
{
    /// <summary>
    /// Registers Kodexia Hub services using a fluent configuration delegate.
    /// </summary>
    public static IServiceCollection AddKodexiaHub(
        this IServiceCollection services,
        Action<HubServiceConfiguration> configure)
    {
        var serviceConfig = new HubServiceConfiguration();
        configure.Invoke(serviceConfig);
        return services.AddKodexiaHub(serviceConfig);
    }

    /// <summary>
    /// Registers Kodexia Hub services using a pre-built configuration.
    /// </summary>
    public static IServiceCollection AddKodexiaHub(
        this IServiceCollection services,
        HubServiceConfiguration configuration)
    {
        if (configuration.AssembliesToRegister.Count == 0)
        {
            throw new ArgumentException(
                "No assemblies found to scan. Supply at least one assembly via " +
                $"{nameof(HubServiceConfiguration)}.{nameof(HubServiceConfiguration.RegisterHubClassesFromAssembly)}.");
        }

        ServiceRegistrar.AddHubClasses(services, configuration);
        ServiceRegistrar.AddRequiredServices(services, configuration);

        return services;
    }
}
