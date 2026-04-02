namespace Kodexia.Cqrs;

/// <summary>
/// Marker interface for notification messages published via the Pub/Sub pattern.
/// Unlike requests, notifications can be handled by zero or more handlers.
/// </summary>
/// <remarks>
/// Notifications are dispatched via <see cref="IPublisher.PublishAsync{TNotification}"/>.
/// Multiple <c>INotificationHandler&lt;TNotification&gt;</c> implementations can react
/// to the same notification independently.
/// </remarks>
/// <example>
/// <code>
/// public record UserRegisteredNotification(Guid UserId, string Email) : INotification;
/// </code>
/// </example>
public interface INotification;
