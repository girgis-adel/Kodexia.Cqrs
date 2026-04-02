namespace Kodexia.Cqrs;

/// <summary>
/// Defines a handler for a notification message of type <typeparamref name="TNotification"/>.
/// Multiple handlers can be registered for the same notification type.
/// </summary>
/// <typeparam name="TNotification">The type of notification being handled. Must implement <see cref="INotification"/>.</typeparam>
/// <remarks>
/// All registered handlers are invoked when the notification is published via
/// <see cref="IPublisher.PublishAsync{TNotification}"/>. The invocation strategy
/// (sequential vs. parallel) is controlled by the configured <see cref="INotificationPublisher"/>.
/// </remarks>
/// <example>
/// <code>
/// public class SendWelcomeEmailHandler : INotificationHandler&lt;UserRegisteredNotification&gt;
/// {
///     public Task HandleAsync(UserRegisteredNotification notification, CancellationToken ct)
///         => _emailService.SendWelcomeAsync(notification.Email, ct);
/// }
/// </code>
/// </example>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Handles the specified notification asynchronously.
    /// </summary>
    /// <param name="notification">The published notification instance.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken);
}

/// <summary>
/// A synchronous base class for <see cref="INotificationHandler{TNotification}"/> that
/// wraps a synchronous <see cref="Handle"/> method in a completed <see cref="Task"/>.
/// </summary>
/// <typeparam name="TNotification">The type of notification being handled.</typeparam>
/// <remarks>
/// Use this base class when your handler logic is inherently synchronous and you want
/// to avoid the boilerplate of returning <see cref="Task.CompletedTask"/>.
/// </remarks>
/// <example>
/// <code>
/// public class AuditHandler : NotificationHandler&lt;OrderShippedNotification&gt;
/// {
///     protected override void Handle(OrderShippedNotification notification)
///         => _auditLog.Record(notification.OrderId);
/// }
/// </code>
/// </example>
public abstract class NotificationHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
    Task INotificationHandler<TNotification>.HandleAsync(TNotification notification, CancellationToken cancellationToken)
    {
        Handle(notification);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Synchronously handles the specified notification.
    /// </summary>
    /// <param name="notification">The published notification instance.</param>
    protected abstract void Handle(TNotification notification);
}
