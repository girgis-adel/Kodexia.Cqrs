namespace Kodexia.Cqrs;

/// <summary>
/// A strategy that broadcasts a signal to all subscribers concurrently.
/// </summary>
public class TaskWhenAllBroadcastStrategy : IBroadcastStrategy
{
    public Task BroadcastAsync(
        IEnumerable<SubscriberExecutor> executors,
        ISignal signal,
        CancellationToken ct)
    {
        var tasks = executors
            .Select(executor => executor.SubscriberCallback(signal, ct))
            .ToArray();

        return Task.WhenAll(tasks);
    }
}
