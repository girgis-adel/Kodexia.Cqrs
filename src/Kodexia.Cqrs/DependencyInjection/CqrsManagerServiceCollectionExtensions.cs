using Kodexia.Cqrs.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Kodexia.Cqrs;

/// <summary>
/// Provides extension methods for registering Kodexia.Cqrs services with the
/// <see cref="IServiceCollection"/>.
/// </summary>
public static class CqrsManagerServiceCollectionExtensions
{
    /// <summary>
    /// Registers Kodexia.Cqrs services — handlers, behaviors, and the <see cref="ICqrsManager"/> —
    /// using a fluent configuration delegate.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configure">A delegate to configure the <see cref="CqrsManagerServiceConfiguration"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when no assemblies are provided for handler scanning.
    /// </exception>
    /// <example>
    /// <code>
    /// builder.Services.AddKodexiaCqrs(cfg =>
    /// {
    ///     cfg.RegisterServicesFromAssemblyContaining&lt;Program&gt;();
    ///     cfg.AddOpenBehavior(typeof(LoggingBehavior&lt;,&gt;));
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddKodexiaCqrs(
        this IServiceCollection services,
        Action<CqrsManagerServiceConfiguration> configure)
    {
        var serviceConfig = new CqrsManagerServiceConfiguration();
        configure.Invoke(serviceConfig);
        return services.AddKodexiaCqrs(serviceConfig);
    }

    /// <summary>
    /// Registers Kodexia.Cqrs services using a pre-built <see cref="CqrsManagerServiceConfiguration"/> instance.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The pre-configured <see cref="CqrsManagerServiceConfiguration"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddKodexiaCqrs(
        this IServiceCollection services,
        CqrsManagerServiceConfiguration configuration)
    {
        if (configuration.AssembliesToRegister.Count == 0)
        {
            throw new ArgumentException(
                "No assemblies found to scan. Supply at least one assembly via " +
                $"{nameof(CqrsManagerServiceConfiguration)}.{nameof(CqrsManagerServiceConfiguration.RegisterServicesFromAssembly)}.");
        }

        ServiceRegistrar.AddCqrsClasses(services, configuration);
        ServiceRegistrar.AddRequiredServices(services, configuration);

        return services;
    }
}
