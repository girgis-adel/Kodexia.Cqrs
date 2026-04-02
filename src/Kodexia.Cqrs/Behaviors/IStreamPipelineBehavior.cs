namespace Kodexia.Cqrs;

/// <summary>
/// A delegate representing the next action in a streaming request processing pipeline.
/// </summary>
/// <typeparam name="TResponse">The type of each item yielded by the stream.</typeparam>
/// <returns>An <see cref="IAsyncEnumerable{T}"/> representing the stream from the next step.</returns>
public delegate IAsyncEnumerable<TResponse> StreamHandlerDelegate<TResponse>();

/// <summary>
/// Defines a pipeline behavior for streaming requests, enabling cross-cutting concerns
/// around <see cref="IAsyncEnumerable{T}"/> handlers.
/// </summary>
/// <typeparam name="TRequest">The type of the stream request message.</typeparam>
/// <typeparam name="TResponse">The type of each item yielded in the stream.</typeparam>
/// <example>
/// <code>
/// public class StreamLoggingBehavior&lt;TRequest, TResponse&gt;
///     : IStreamPipelineBehavior&lt;TRequest, TResponse&gt;
///     where TRequest : notnull
/// {
///     public async IAsyncEnumerable&lt;TResponse&gt; HandleAsync(
///         TRequest request, StreamHandlerDelegate&lt;TResponse&gt; next,
///         [EnumeratorCancellation] CancellationToken ct)
///     {
///         _logger.LogInformation("Streaming {Request}", typeof(TRequest).Name);
///         await foreach (var item in next().WithCancellation(ct))
///             yield return item;
///     }
/// }
/// </code>
/// </example>
public interface IStreamPipelineBehavior<in TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Handles the streaming pipeline step, optionally transforming or filtering items.
    /// Must call <paramref name="next"/> to produce the inner stream.
    /// </summary>
    /// <param name="request">The current stream request instance.</param>
    /// <param name="next">A delegate to invoke the next step in the streaming pipeline.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of response items.</returns>
    IAsyncEnumerable<TResponse> HandleAsync(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
