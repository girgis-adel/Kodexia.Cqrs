namespace Kodexia.Cqrs;

/// <summary>
/// Encapsulates a notification handler instance and its strongly-typed callback delegate,
/// allowing the publisher to invoke handlers without knowing their concrete type.
/// </summary>
/// <param name="HandlerInstance">The resolved handler instance.</param>
/// <param name="HandlerCallback">
/// A delegate that invokes the handler's <c>HandleAsync</c> method with the notification and cancellation token.
/// </param>
public record NotificationHandlerExecutor(
    object HandlerInstance,
    Func<INotification, CancellationToken, Task> HandlerCallback);
