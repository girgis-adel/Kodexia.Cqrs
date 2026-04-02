namespace Kodexia.Cqrs;

/// <summary>
/// Defines a subscriber for a signal (broadcast message).
/// </summary>
public interface ISubscriber<in TSignal>
    where TSignal : ISignal
{
    Task OnSignalAsync(TSignal signal, CancellationToken ct);
}

/// <summary>
/// A base class for synchronous subscribers.
/// </summary>
public abstract class Subscriber<TSignal> : ISubscriber<TSignal>
    where TSignal : ISignal
{
    Task ISubscriber<TSignal>.OnSignalAsync(TSignal signal, CancellationToken ct)
    {
        React(signal);
        return Task.CompletedTask;
    }

    protected abstract void React(TSignal signal);
}
