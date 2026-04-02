using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

namespace Kodexia.Cqrs.Internal;

/// <summary>
/// A built-in interceptor that handles exceptions and provides fallback responses
/// via registered <see cref="IFallbackHandler{TMessage, TResponse, TException}"/> implementations.
/// </summary>
internal sealed class FallbackInterceptor<TMessage, TResponse>(IServiceProvider serviceProvider)
    : IInterceptor<TMessage, TResponse>
    where TMessage : IMessage<TResponse>
{
    private static readonly ConcurrentDictionary<Type, FallbackDispatcher<TMessage, TResponse>>
        _dispatcherCache = new();

    public async Task<TResponse> InterceptAsync(IMessageContext<TMessage, TResponse> context, CancellationToken ct)
    {
        try
        {
            return await context.NextAsync(ct).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            var state = new FallbackState<TResponse>();
            var exceptionTypes = GetExceptionTypeHierarchy(exception.GetType());

            foreach (var exceptionType in exceptionTypes)
            {
                var dispatcher = _dispatcherCache.GetOrAdd(
                    exceptionType,
                    static et => FallbackDispatcherFactory.Create<TMessage, TResponse>(et));

                await dispatcher.DispatchAsync(serviceProvider, context.Message, exception, state, ct)
                    .ConfigureAwait(false);

                if (state.Handled)
                    break;
            }

            if (!state.Handled || state.Response is null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }

            return state.Response!;
        }
    }

    private static IEnumerable<Type> GetExceptionTypeHierarchy(Type? exceptionType)
    {
        while (exceptionType is not null && exceptionType != typeof(object))
        {
            yield return exceptionType;
            exceptionType = exceptionType.BaseType;
        }
    }
}
