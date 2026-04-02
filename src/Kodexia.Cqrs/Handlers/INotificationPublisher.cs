namespace Kodexia.Cqrs;

/// <summary>
/// Defines the strategy used to invoke notification handlers when a notification is published.
/// </summary>
/// <remarks>
/// Register a custom publisher via <c>CqrsManagerServiceConfiguration.NotificationPublisherType</c>
/// or <c>CqrsManagerServiceConfiguration.NotificationPublisher</c>.
/// Two built-in implementations are provided:
/// <list type="bullet">
///   <item><see cref="ForeachAwaitPublisher"/> — invokes handlers sequentially (default).</item>
///   <item><see cref="TaskWhenAllPublisher"/> — invokes all handlers concurrently.</item>
/// </list>
/// </remarks>
public interface INotificationPublisher
{
    /// <summary>
    /// Publishes the <paramref name="notification"/> to all registered handler executors.
    /// </summary>
    /// <param name="handlerExecutors">
    /// The collection of handler executors resolved for the notification type.
    /// Each executor wraps the underlying handler and its strongly-typed callback.
    /// </param>
    /// <param name="notification">The notification instance being published.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <returns>A <see cref="Task"/> that represents the completion of all handler invocations.</returns>
    Task PublishAsync(
        IEnumerable<NotificationHandlerExecutor> handlerExecutors,
        INotification notification,
        CancellationToken cancellationToken);
}
