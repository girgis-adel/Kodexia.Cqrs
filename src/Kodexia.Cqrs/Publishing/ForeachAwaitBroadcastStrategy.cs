namespace Kodexia.Cqrs;

/// <summary>
/// A strategy that broadcasts a signal to subscribers sequentially.
/// </summary>
public class ForeachAwaitBroadcastStrategy : IBroadcastStrategy
{
    public async Task BroadcastAsync(
        IEnumerable<SubscriberExecutor> executors,
        ISignal signal,
        CancellationToken ct)
    {
        foreach (var executor in executors)
        {
            await executor.SubscriberCallback(signal, ct).ConfigureAwait(false);
        }
    }
}
