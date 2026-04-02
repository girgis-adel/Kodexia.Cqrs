namespace Kodexia.Cqrs;

/// <summary>
/// Defines the ability to broadcast signals to all registered subscribers.
/// </summary>
public interface IBroadcastAgent
{
    /// <summary>
    /// Broadcasts an untyped signal.
    /// </summary>
    Task BroadcastAsync(object signal, CancellationToken ct = default);

    /// <summary>
    /// Broadcasts a strongly-typed signal.
    /// </summary>
    Task BroadcastAsync<TSignal>(TSignal signal, CancellationToken ct = default)
        where TSignal : ISignal;
}
