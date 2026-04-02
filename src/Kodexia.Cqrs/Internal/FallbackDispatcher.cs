using Microsoft.Extensions.DependencyInjection;

namespace Kodexia.Cqrs.Internal;

/// <summary>
/// Strongly-typed cached dispatcher for <see cref="IFallbackHandler{TMessage, TResponse, TException}"/>.
/// </summary>
internal abstract class FallbackDispatcher<TMessage, TResponse>
    where TMessage : notnull
{
    public abstract Task DispatchAsync(
        IServiceProvider serviceProvider,
        TMessage message,
        Exception exception,
        FallbackState<TResponse> state,
        CancellationToken ct);
}

internal sealed class FallbackDispatcherImpl<TMessage, TResponse, TException>
    : FallbackDispatcher<TMessage, TResponse>
    where TMessage : notnull
    where TException : Exception
{
    public override async Task DispatchAsync(
        IServiceProvider serviceProvider,
        TMessage message,
        Exception exception,
        FallbackState<TResponse> state,
        CancellationToken ct)
    {
        var handlers = serviceProvider.GetServices<IFallbackHandler<TMessage, TResponse, TException>>();
        var handlerList = handlers as IFallbackHandler<TMessage, TResponse, TException>[] ?? [.. handlers];

        if (handlerList.Length == 0)
            return;

        var orderedHandlers = HandlersOrderer.Prioritize<TMessage>(
            handlerList.Cast<object>().ToList(), message);

        foreach (var handlerObj in orderedHandlers)
        {
            var handler = (IFallbackHandler<TMessage, TResponse, TException>)handlerObj;
            await handler.HandleAsync(message, (TException)exception, state, ct).ConfigureAwait(false);

            if (state.Handled)
                break;
        }
    }
}

internal static class FallbackDispatcherFactory
{
    public static FallbackDispatcher<TMessage, TResponse> Create<TMessage, TResponse>(Type exceptionType)
        where TMessage : notnull
    {
        var dispatcherType = typeof(FallbackDispatcherImpl<,,>)
            .MakeGenericType(typeof(TMessage), typeof(TResponse), exceptionType);

        return (FallbackDispatcher<TMessage, TResponse>)Activator.CreateInstance(dispatcherType)!;
    }
}
