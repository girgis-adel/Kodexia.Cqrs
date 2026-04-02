namespace Kodexia.Cqrs;

/// <summary>
/// Defines a pre-processor that runs before the main request handler and all pipeline behaviors.
/// Use this for lightweight setup operations such as validation, logging request metadata, or enriching context.
/// </summary>
/// <typeparam name="TRequest">The type of the request being pre-processed.</typeparam>
/// <remarks>
/// Pre-processors are applied before any <see cref="IPipelineBehavior{TRequest, TResponse}"/>.
/// They are automatically wrapped in a <c>RequestPreProcessorBehavior</c> when registered.
/// </remarks>
/// <example>
/// <code>
/// public class RequestValidationPreProcessor&lt;TRequest&gt; : IRequestPreProcessor&lt;TRequest&gt;
///     where TRequest : notnull
/// {
///     public Task ProcessAsync(TRequest request, CancellationToken ct)
///     {
///         // Throw if invalid
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface IRequestPreProcessor<in TRequest>
    where TRequest : notnull
{
    /// <summary>
    /// Runs pre-processing logic before the request is dispatched to the handler pipeline.
    /// </summary>
    /// <param name="request">The incoming request instance.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous pre-processing operation.</returns>
    Task ProcessAsync(TRequest request, CancellationToken cancellationToken);
}
