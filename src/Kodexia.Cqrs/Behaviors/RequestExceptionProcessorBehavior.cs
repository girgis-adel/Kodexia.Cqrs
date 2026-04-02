using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using Kodexia.Cqrs.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Kodexia.Cqrs;

/// <summary>
/// A built-in pipeline behavior that intercepts exceptions and dispatches them to registered
/// <see cref="IRequestExceptionHandler{TRequest, TResponse, TException}"/> implementations.
/// </summary>
/// <typeparam name="TRequest">The type of the request being processed.</typeparam>
/// <typeparam name="TResponse">The type of the response expected from the handler.</typeparam>
public class RequestExceptionProcessorBehavior<TRequest, TResponse>(IServiceProvider serviceProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    // Cache: exceptionType → strongly-typed dispatcher delegate
    // Avoids MethodInfo.Invoke on every call after the first.
    private static readonly ConcurrentDictionary<Type, ExceptionHandlerDispatcher<TRequest, TResponse>>
        _dispatcherCache = new();

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            var state = new RequestExceptionHandlerState<TResponse>();
            var exceptionTypes = GetExceptionTypeHierarchy(exception.GetType());

            foreach (var exceptionType in exceptionTypes)
            {
                var dispatcher = _dispatcherCache.GetOrAdd(
                    exceptionType,
                    static et => ExceptionHandlerDispatcherFactory.Create<TRequest, TResponse>(et));

                await dispatcher.DispatchAsync(_serviceProvider, request, exception, state, cancellationToken)
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
