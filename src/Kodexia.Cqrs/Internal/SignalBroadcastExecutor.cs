using Microsoft.Extensions.DependencyInjection;

namespace Kodexia.Cqrs.Internal;

internal abstract class SignalBroadcastExecutor
{
    public abstract Task BroadcastAsync(
        ISignal signal,
        IServiceProvider serviceProvider,
        Func<IEnumerable<SubscriberExecutor>, ISignal, CancellationToken, Task> broadcast,
        CancellationToken ct);
}

internal sealed class SignalBroadcastExecutorImpl<TSignal> : SignalBroadcastExecutor
    where TSignal : ISignal
{
    public override Task BroadcastAsync(
        ISignal signal,
        IServiceProvider serviceProvider,
        Func<IEnumerable<SubscriberExecutor>, ISignal, CancellationToken, Task> broadcast,
        CancellationToken ct)
    {
        var services = serviceProvider.GetServices<ISubscriber<TSignal>>();
        var subscribers = services as ISubscriber<TSignal>[] ?? [.. services];

        if (subscribers.Length == 0)
            return Task.CompletedTask;

        var seenTypes = new HashSet<Type>(subscribers.Length);
        var uniqueSubscribers = new List<ISubscriber<TSignal>>(subscribers.Length);

        foreach (var subscriber in subscribers)
        {
            if (seenTypes.Add(subscriber.GetType()))
                uniqueSubscribers.Add(subscriber);
        }

        var executors = new SubscriberExecutor[uniqueSubscribers.Count];

        for (var i = 0; i < uniqueSubscribers.Count; i++)
        {
            var subscriber = uniqueSubscribers[i];
            executors[i] = new SubscriberExecutor(
                subscriber,
                (theSignal, theToken) => subscriber.OnSignalAsync((TSignal)theSignal, theToken));
        }

        return broadcast(executors, signal, ct);
    }
}
