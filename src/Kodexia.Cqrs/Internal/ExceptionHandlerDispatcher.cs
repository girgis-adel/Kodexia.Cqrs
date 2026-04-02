using Microsoft.Extensions.DependencyInjection;

namespace Kodexia.Cqrs.Internal;

/// <summary>
/// A strongly-typed, cached dispatcher that resolves and invokes
/// <see cref="IRequestExceptionHandler{TRequest, TResponse, TException}"/> instances
/// without reflection at call-time.
/// </summary>
internal abstract class ExceptionHandlerDispatcher<TRequest, TResponse>
    where TRequest : notnull
{
    public abstract Task DispatchAsync(
        IServiceProvider serviceProvider,
        TRequest request,
        Exception exception,
        RequestExceptionHandlerState<TResponse> state,
        CancellationToken cancellationToken);
}

/// <summary>
/// Concrete strongly-typed dispatcher for a specific exception type.
/// Created once per (TRequest, TResponse, TException) combination and cached.
/// </summary>
internal sealed class ExceptionHandlerDispatcher<TRequest, TResponse, TException>
    : ExceptionHandlerDispatcher<TRequest, TResponse>
    where TRequest : notnull
    where TException : Exception
{
    public override async Task DispatchAsync(
        IServiceProvider serviceProvider,
        TRequest request,
        Exception exception,
        RequestExceptionHandlerState<TResponse> state,
        CancellationToken cancellationToken)
    {
        var handlers = serviceProvider
            .GetServices<IRequestExceptionHandler<TRequest, TResponse, TException>>();

        var handlerList = handlers as IRequestExceptionHandler<TRequest, TResponse, TException>[]
            ?? [.. handlers];

        if (handlerList.Length == 0)
            return;

        var orderedHandlers = HandlersOrderer.Prioritize<TRequest>(
            handlerList.Cast<object>().ToList(), request);

        foreach (var handlerObj in orderedHandlers)
        {
            var handler = (IRequestExceptionHandler<TRequest, TResponse, TException>)handlerObj;

            await handler.HandleAsync(request, (TException)exception, state, cancellationToken)
                .ConfigureAwait(false);

            if (state.Handled)
                break;
        }
    }
}

/// <summary>
/// Factory that creates <see cref="ExceptionHandlerDispatcher{TRequest,TResponse}"/> instances
/// for a given exception type using a single Activator.CreateInstance call (cached after first use).
/// </summary>
internal static class ExceptionHandlerDispatcherFactory
{
    public static ExceptionHandlerDispatcher<TRequest, TResponse> Create<TRequest, TResponse>(Type exceptionType)
        where TRequest : notnull
    {
        var dispatcherType = typeof(ExceptionHandlerDispatcher<,,>)
            .MakeGenericType(typeof(TRequest), typeof(TResponse), exceptionType);

        return (ExceptionHandlerDispatcher<TRequest, TResponse>)(
            Activator.CreateInstance(dispatcherType)
            ?? throw new InvalidOperationException(
                $"Could not create exception dispatcher for exception type '{exceptionType.FullName}'."));
    }
}
