namespace Kodexia.Cqrs;

/// <summary>
/// Defines the strategy used to invoke subscribers when a signal is broadcast.
/// </summary>
public interface IBroadcastStrategy
{
    /// <summary>
    /// Broadcasts the <paramref name="signal"/> to all registered subscriber executors.
    /// </summary>
    Task BroadcastAsync(
        IEnumerable<SubscriberExecutor> executors,
        ISignal signal,
        CancellationToken ct);
}
