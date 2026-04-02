namespace Kodexia.Cqrs;

/// <summary>
/// Defines a side-effect action to perform when an exception occurs during request processing.
/// Unlike <see cref="IRequestExceptionHandler{TRequest, TResponse, TException}"/>, an action
/// cannot mark the exception as handled or supply a response — it is fire-and-recover only.
/// </summary>
/// <typeparam name="TRequest">The type of the request that caused the exception.</typeparam>
/// <typeparam name="TException">The specific exception type to react to.</typeparam>
/// <remarks>
/// Use exception actions for cross-cutting side effects such as alerting, metric recording,
/// or corrective background tasks, when you do not need to suppress or replace the exception.
/// </remarks>
/// <example>
/// <code>
/// public class MetricsExceptionAction
///     : IRequestExceptionAction&lt;CreateOrderCommand, TimeoutException&gt;
/// {
///     public Task Execute(CreateOrderCommand request, TimeoutException ex, CancellationToken ct)
///     {
///         _metrics.IncrementCounter("order.timeout");
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface IRequestExceptionAction<in TRequest, in TException>
    where TRequest : notnull
    where TException : Exception
{
    /// <summary>
    /// Executes the action in response to the specified exception.
    /// </summary>
    /// <param name="request">The original request that triggered the exception.</param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous action.</returns>
    Task Execute(TRequest request, TException exception, CancellationToken cancellationToken);
}
