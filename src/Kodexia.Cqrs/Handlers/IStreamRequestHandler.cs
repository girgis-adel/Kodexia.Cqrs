namespace Kodexia.Cqrs;

/// <summary>
/// Defines a handler for a streaming request that asynchronously yields multiple
/// responses of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TRequest">The type of the stream request. Must implement <see cref="IStreamRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of each item yielded by the stream.</typeparam>
/// <example>
/// <code>
/// public class GetLogsHandler : IStreamRequestHandler&lt;GetLogsRequest, LogEntry&gt;
/// {
///     public async IAsyncEnumerable&lt;LogEntry&gt; HandleAsync(
///         GetLogsRequest request,
///         [EnumeratorCancellation] CancellationToken cancellationToken)
///     {
///         await foreach (var entry in _logStore.ReadAsync(cancellationToken))
///             yield return entry;
///     }
/// }
/// </code>
/// </example>
public interface IStreamRequestHandler<in TRequest, out TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    /// <summary>
    /// Handles the streaming request and asynchronously yields response items.
    /// </summary>
    /// <param name="request">The incoming stream request instance.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> that yields response items.</returns>
    IAsyncEnumerable<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
