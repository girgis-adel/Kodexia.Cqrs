using Microsoft.Extensions.DependencyInjection;

namespace Kodexia.Cqrs.Internal;

internal abstract class NotificationHandlerWrapper
{
    public abstract Task HandleAsync(
        INotification notification,
        IServiceProvider serviceProvider,
        Func<IEnumerable<NotificationHandlerExecutor>, INotification, CancellationToken, Task> publish,
        CancellationToken cancellationToken);
}

internal sealed class NotificationHandlerWrapperImpl<TNotification> : NotificationHandlerWrapper
    where TNotification : INotification
{
    public override Task HandleAsync(
        INotification notification,
        IServiceProvider serviceProvider,
        Func<IEnumerable<NotificationHandlerExecutor>, INotification, CancellationToken, Task> publish,
        CancellationToken cancellationToken)
    {
        var services = serviceProvider.GetServices<INotificationHandler<TNotification>>();
        var handlers = services as INotificationHandler<TNotification>[] ?? [.. services];

        if (handlers.Length == 0)
            return Task.CompletedTask;

        // Use a HashSet<Type> for O(1) deduplication instead of DistinctBy which allocates a grouping
        var seenTypes = new HashSet<Type>(handlers.Length);
        var uniqueHandlers = new List<INotificationHandler<TNotification>>(handlers.Length);

        foreach (var handler in handlers)
        {
            if (seenTypes.Add(handler.GetType()))
                uniqueHandlers.Add(handler);
        }

        var executors = new NotificationHandlerExecutor[uniqueHandlers.Count];

        for (var i = 0; i < uniqueHandlers.Count; i++)
        {
            var handler = uniqueHandlers[i];
            executors[i] = new NotificationHandlerExecutor(
                handler,
                (theNotification, theToken) => handler.HandleAsync((TNotification)theNotification, theToken));
        }

        return publish(executors, notification, cancellationToken);
    }
}
