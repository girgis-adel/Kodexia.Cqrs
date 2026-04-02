using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

namespace Kodexia.Cqrs.Internal;

/// <summary>
/// A built-in interceptor that observes exceptions and dispatches them to registered
/// <see cref="IExceptionObserver{TMessage, TException}"/> implementations.
/// </summary>
internal sealed class ExceptionObservationInterceptor<TMessage, TResponse>(IServiceProvider serviceProvider)
    : IInterceptor<TMessage, TResponse>
    where TMessage : IMessage<TResponse>
{
    private static readonly ConcurrentDictionary<Type, ExceptionObservationDispatcher<TMessage>>
        _dispatcherCache = new();

    public async Task<TResponse> InterceptAsync(IMessageContext<TMessage, TResponse> context, CancellationToken ct)
    {
        try
        {
            return await context.NextAsync(ct).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            var exceptionTypes = GetExceptionTypeHierarchy(exception.GetType());

            foreach (var exceptionType in exceptionTypes)
            {
                var dispatcher = _dispatcherCache.GetOrAdd(
                    exceptionType,
                    static et => ExceptionObservationDispatcherFactory.Create<TMessage>(et));

                await dispatcher.DispatchAsync(serviceProvider, context.Message, exception, ct)
                    .ConfigureAwait(false);
            }

            ExceptionDispatchInfo.Capture(exception).Throw();
            throw; // unreachable
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
