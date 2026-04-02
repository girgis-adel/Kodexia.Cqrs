using Microsoft.Extensions.DependencyInjection;

namespace Kodexia.Cqrs.Internal;

/// <summary>
/// Strongly-typed cached dispatcher for <see cref="IExceptionObserver{TMessage, TException}"/>.
/// </summary>
internal abstract class ExceptionObservationDispatcher<TMessage>
    where TMessage : notnull
{
    public abstract Task DispatchAsync(
        IServiceProvider serviceProvider,
        TMessage message,
        Exception exception,
        CancellationToken ct);
}

internal sealed class ExceptionObservationDispatcherImpl<TMessage, TException>
    : ExceptionObservationDispatcher<TMessage>
    where TMessage : notnull
    where TException : Exception
{
    public override async Task DispatchAsync(
        IServiceProvider serviceProvider,
        TMessage message,
        Exception exception,
        CancellationToken ct)
    {
        var observers = serviceProvider.GetServices<IExceptionObserver<TMessage, TException>>();
        var observerList = observers as IExceptionObserver<TMessage, TException>[] ?? [.. observers];

        if (observerList.Length == 0)
            return;

        var orderedObservers = HandlersOrderer.Prioritize<TMessage>(
            observerList.Cast<object>().ToList(), message);

        foreach (var observerObj in orderedObservers)
        {
            var observer = (IExceptionObserver<TMessage, TException>)observerObj;
            await observer.ObserveAsync(message, (TException)exception, ct).ConfigureAwait(false);
        }
    }
}

internal static class ExceptionObservationDispatcherFactory
{
    public static ExceptionObservationDispatcher<TMessage> Create<TMessage>(Type exceptionType)
        where TMessage : notnull
    {
        var dispatcherType = typeof(ExceptionObservationDispatcherImpl<,>).MakeGenericType(typeof(TMessage), exceptionType);
        return (ExceptionObservationDispatcher<TMessage>)Activator.CreateInstance(dispatcherType)!;
    }
}
