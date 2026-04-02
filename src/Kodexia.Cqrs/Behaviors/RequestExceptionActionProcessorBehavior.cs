using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using Kodexia.Cqrs.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Kodexia.Cqrs;

/// <summary>
/// A built-in pipeline behavior that intercepts exceptions and dispatches them to registered
/// <see cref="IRequestExceptionAction{TRequest, TException}"/> implementations as side-effect
/// actions that cannot suppress the exception.
/// </summary>
/// <typeparam name="TRequest">The type of the request being processed.</typeparam>
/// <typeparam name="TResponse">The type of the response expected from the handler.</typeparam>
/// <remarks>
/// The order of exception action processors vs. exception handlers is controlled by
/// <see cref="RequestExceptionActionProcessorStrategy"/>.
/// </remarks>
public class RequestExceptionActionProcessorBehavior<TRequest, TResponse>(IServiceProvider serviceProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private static readonly ConcurrentDictionary<Type, ExceptionActionDispatcher<TRequest>>
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
            var exceptionTypes = GetExceptionTypeHierarchy(exception.GetType());

            foreach (var exceptionType in exceptionTypes)
            {
                var dispatcher = _dispatcherCache.GetOrAdd(
                    exceptionType,
                    static et => ExceptionActionDispatcherFactory.Create<TRequest>(et));

                await dispatcher.DispatchAsync(_serviceProvider, request, exception, cancellationToken)
                    .ConfigureAwait(false);
            }

            ExceptionDispatchInfo.Capture(exception).Throw();
            throw; // unreachable — satisfies compiler
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
