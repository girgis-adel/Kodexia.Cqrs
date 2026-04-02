namespace Kodexia.Cqrs;

/// <summary>
/// Defines a post-processor that runs after the main request handler and all pipeline behaviors.
/// Use this for cleanup, audit logging, metric recording, or response enrichment.
/// </summary>
/// <typeparam name="TRequest">The type of the request that was processed.</typeparam>
/// <typeparam name="TResponse">The type of the response produced by the handler.</typeparam>
/// <example>
/// <code>
/// public class AuditPostProcessor&lt;TRequest, TResponse&gt; : IRequestPostProcessor&lt;TRequest, TResponse&gt;
///     where TRequest : notnull
/// {
///     public Task ProcessAsync(TRequest request, TResponse response, CancellationToken ct)
///     {
///         _audit.Log(typeof(TRequest).Name, response);
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface IRequestPostProcessor<in TRequest, in TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Runs post-processing logic after the handler has produced a response.
    /// </summary>
    /// <param name="request">The original request instance.</param>
    /// <param name="response">The response produced by the handler.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous post-processing operation.</returns>
    Task ProcessAsync(TRequest request, TResponse response, CancellationToken cancellationToken);
}
