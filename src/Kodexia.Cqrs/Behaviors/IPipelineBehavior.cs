namespace Kodexia.Cqrs;

/// <summary>
/// A delegate representing the next action in a request processing pipeline.
/// Invoke this delegate to pass control to the next behavior or the final handler.
/// </summary>
/// <typeparam name="TResponse">The type of the response returned by the handler.</typeparam>
/// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
/// <returns>A <see cref="Task{TResult}"/> representing the result from the next pipeline step.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);

/// <summary>
/// Defines a pipeline behavior that wraps the inner request handler, enabling
/// cross-cutting concerns such as logging, validation, caching, and retry logic.
/// </summary>
/// <typeparam name="TRequest">The type of the request message.</typeparam>
/// <typeparam name="TResponse">The type of the response message.</typeparam>
/// <remarks>
/// <para>
/// Behaviors are applied in registration order. The first registered behavior is the outermost
/// wrapper; the last registered behavior directly wraps the handler.
/// </para>
/// <para>
/// Register behaviors via <c>CqrsManagerServiceConfiguration.AddBehavior</c> or
/// <c>AddOpenBehavior</c> for generic open-type registrations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class LoggingBehavior&lt;TRequest, TResponse&gt; : IPipelineBehavior&lt;TRequest, TResponse&gt;
///     where TRequest : notnull
/// {
///     public async Task&lt;TResponse&gt; HandleAsync(
///         TRequest request, RequestHandlerDelegate&lt;TResponse&gt; next, CancellationToken ct)
///     {
///         _logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
///         var response = await next(ct);
///         _logger.LogInformation("Handled {Request}", typeof(TRequest).Name);
///         return response;
///     }
/// }
/// </code>
/// </example>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Handles the pipeline step for the given <paramref name="request"/>.
    /// Must call <paramref name="next"/> to continue the pipeline, or short-circuit by returning directly.
    /// </summary>
    /// <param name="request">The current request instance.</param>
    /// <param name="next">A delegate to invoke the next step in the pipeline.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> containing the <typeparamref name="TResponse"/>,
    /// either from the next pipeline step or from a short-circuit.
    /// </returns>
    Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
