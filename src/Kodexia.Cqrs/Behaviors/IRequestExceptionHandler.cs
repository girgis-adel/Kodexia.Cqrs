namespace Kodexia.Cqrs;

/// <summary>
/// Defines a handler for exceptions thrown during the processing of a request.
/// Implement this to provide fine-grained exception recovery logic without try/catch in application code.
/// </summary>
/// <typeparam name="TRequest">The type of the request that caused the exception.</typeparam>
/// <typeparam name="TResponse">The type of the response the handler would have produced.</typeparam>
/// <typeparam name="TException">The specific exception type to handle.</typeparam>
/// <remarks>
/// Exception handlers are ordered by proximity: handlers in the same assembly and namespace
/// as the request are preferred over handlers registered from external assemblies.
/// Set <see cref="RequestExceptionHandlerState{TResponse}.Handled"/> to <see langword="true"/>
/// and provide a <see cref="RequestExceptionHandlerState{TResponse}.SetHandled"/> response
/// to prevent the exception from propagating.
/// </remarks>
/// <example>
/// <code>
/// public class NotFoundExceptionHandler
///     : IRequestExceptionHandler&lt;GetOrderQuery, OrderDto, NotFoundException&gt;
/// {
///     public Task HandleAsync(GetOrderQuery request, NotFoundException ex,
///         RequestExceptionHandlerState&lt;OrderDto&gt; state, CancellationToken ct)
///     {
///         state.SetHandled(OrderDto.Empty);
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface IRequestExceptionHandler<in TRequest, TResponse, in TException>
    where TRequest : notnull
    where TException : Exception
{
    /// <summary>
    /// Handles the specified exception thrown during request processing.
    /// </summary>
    /// <param name="request">The original request that triggered the exception.</param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <param name="state">
    /// The mutable state object used to signal whether the exception has been handled
    /// and optionally provide a fallback response.
    /// </param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous exception handling operation.</returns>
    Task HandleAsync(
        TRequest request,
        TException exception,
        RequestExceptionHandlerState<TResponse> state,
        CancellationToken cancellationToken);
}
