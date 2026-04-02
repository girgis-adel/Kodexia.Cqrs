namespace Kodexia.Cqrs;

/// <summary>
/// An <see cref="INotificationPublisher"/> that invokes all notification handlers <strong>concurrently</strong>
/// using <see cref="Task.WhenAll(IEnumerable{Task})"/>.
/// </summary>
/// <remarks>
/// <para>
/// All handlers are started simultaneously. If any handler throws, the aggregate exception is
/// surfaced after all handlers complete (or fault). Use this publisher when handler order does
/// not matter and throughput is more important than ordering guarantees.
/// </para>
/// <para>
/// Consider the failure semantics carefully: a fault in one handler does <strong>not</strong>
/// cancel other in-flight handlers.
/// </para>
/// </remarks>
public class TaskWhenAllPublisher : INotificationPublisher
{
    /// <inheritdoc />
    public Task PublishAsync(
        IEnumerable<NotificationHandlerExecutor> handlerExecutors,
        INotification notification,
        CancellationToken cancellationToken)
    {
        var tasks = handlerExecutors
            .Select(handler => handler.HandlerCallback(notification, cancellationToken))
            .ToArray();

        return Task.WhenAll(tasks);
    }
}
