namespace Kodexia.Cqrs;

/// <summary>
/// An <see cref="INotificationPublisher"/> that invokes notification handlers <strong>sequentially</strong>,
/// awaiting each one before proceeding to the next.
/// </summary>
/// <remarks>
/// This is the <strong>default</strong> publisher strategy. It guarantees handler execution order
/// and simplifies exception semantics — an exception in one handler stops subsequent handlers.
/// </remarks>
public class ForeachAwaitPublisher : INotificationPublisher
{
    /// <inheritdoc />
    public async Task PublishAsync(
        IEnumerable<NotificationHandlerExecutor> handlerExecutors,
        INotification notification,
        CancellationToken cancellationToken)
    {
        foreach (var handler in handlerExecutors)
        {
            await handler.HandlerCallback(notification, cancellationToken).ConfigureAwait(false);
        }
    }
}
