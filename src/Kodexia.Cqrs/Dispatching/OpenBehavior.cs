using Microsoft.Extensions.DependencyInjection;

namespace Kodexia.Cqrs;

/// <summary>
/// Configures an open generic pipeline behavior to be registered with the DI container.
/// </summary>
/// <remarks>
/// Use this class with <c>CqrsManagerServiceConfiguration.AddOpenBehaviors</c> to register
/// open generic behaviors with specific service lifetimes.
/// </remarks>
/// <example>
/// <code>
/// services.AddKodexiaCqrs(cfg =>
/// {
///     cfg.RegisterServicesFromAssemblyContaining&lt;Program&gt;();
///     cfg.AddOpenBehaviors(new[]
///     {
///         new OpenBehavior(typeof(LoggingBehavior&lt;,&gt;)),
///         new OpenBehavior(typeof(ValidationBehavior&lt;,&gt;), ServiceLifetime.Singleton)
///     });
/// });
/// </code>
/// </example>
public sealed class OpenBehavior
{
    /// <summary>
    /// Initializes a new <see cref="OpenBehavior"/> with the specified open generic behavior type and lifetime.
    /// </summary>
    /// <param name="openBehaviorType">
    /// The open generic type that implements <see cref="IPipelineBehavior{TRequest,TResponse}"/>.
    /// Must be a generic type definition (e.g., <c>typeof(LoggingBehavior&lt;,&gt;)</c>).
    /// </param>
    /// <param name="serviceLifetime">The DI lifetime for the registered behavior. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="openBehaviorType"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="openBehaviorType"/> is not generic or does not implement <see cref="IPipelineBehavior{TRequest,TResponse}"/>.
    /// </exception>
    public OpenBehavior(Type openBehaviorType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        ValidatePipelineBehaviorType(openBehaviorType);
        OpenBehaviorType = openBehaviorType;
        ServiceLifetime = serviceLifetime;
    }

    /// <summary>Gets the open generic pipeline behavior type.</summary>
    public Type OpenBehaviorType { get; }

    /// <summary>Gets the DI service lifetime for this behavior.</summary>
    public ServiceLifetime ServiceLifetime { get; }

    private static void ValidatePipelineBehaviorType(Type openBehaviorType)
    {
        ArgumentNullException.ThrowIfNull(openBehaviorType);

        if (!openBehaviorType.IsGenericType)
        {
            throw new InvalidOperationException(
                $"The type '{openBehaviorType.Name}' must be a generic type definition " +
                $"(e.g., typeof(MyBehavior<,>)).");
        }

        var isPipelineBehavior = openBehaviorType.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

        if (!isPipelineBehavior)
        {
            throw new InvalidOperationException(
                $"The type '{openBehaviorType.Name}' must implement {typeof(IPipelineBehavior<,>).FullName}.");
        }
    }
}
