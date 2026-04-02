namespace Kodexia.Cqrs;

/// <summary>
/// Defines the ability to publish notification messages to all registered handlers.
/// </summary>
public interface IPublisher
{
    /// <summary>
    /// Publishes an untyped notification to all registered handlers for its concrete type.
    /// The object must implement <see cref="INotification"/>.
    /// </summary>
    /// <param name="notification">The notification instance to publish.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <returns>A <see cref="Task"/> that completes when all handlers finish.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="notification"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="notification"/> does not implement <see cref="INotification"/>.</exception>
    Task PublishAsync(object notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a strongly-typed notification to all registered <see cref="INotificationHandler{TNotification}"/> implementations.
    /// </summary>
    /// <typeparam name="TNotification">The concrete notification type. Must implement <see cref="INotification"/>.</typeparam>
    /// <param name="notification">The notification instance to publish.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <returns>A <see cref="Task"/> that completes when all handlers finish.</returns>
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
