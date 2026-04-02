namespace Kodexia.Cqrs;

/// <summary>
/// Encapsulates a subscriber instance and its strongly-typed callback delegate.
/// </summary>
public record SubscriberExecutor(
    object SubscriberInstance,
    Func<ISignal, CancellationToken, Task> SubscriberCallback);
