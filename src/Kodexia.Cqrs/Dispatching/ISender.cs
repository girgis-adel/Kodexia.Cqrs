namespace Kodexia.Cqrs;

/// <summary>
/// Defines the ability to send requests and create async streams through the CQRS pipeline.
/// </summary>
public interface ISender
{
    /// <summary>
    /// Dispatches a request through the pipeline and returns the response asynchronously.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="request">The request to send. Must implement <see cref="IRequest{TResponse}"/>.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the handler's response.</returns>
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatches a void request through the pipeline.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request. Must implement <see cref="IRequest"/>.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <returns>A <see cref="Task"/> that completes when the handler finishes.</returns>
    Task SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest;

    /// <summary>
    /// Dispatches an untyped request object through the pipeline.
    /// The request must implement either <see cref="IRequest{TResponse}"/> or <see cref="IRequest"/>.
    /// </summary>
    /// <param name="request">The untyped request instance.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> containing the boxed response, or <see langword="null"/>
    /// for void requests.
    /// </returns>
    Task<object?> SendAsync(object request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an asynchronous stream by dispatching a streaming request through the pipeline.
    /// </summary>
    /// <typeparam name="TResponse">The type of each item yielded by the stream.</typeparam>
    /// <param name="request">The stream request. Must implement <see cref="IStreamRequest{TResponse}"/>.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> that yields response items.</returns>
    IAsyncEnumerable<TResponse> CreateStreamAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an asynchronous stream from an untyped stream request.
    /// The request must implement <see cref="IStreamRequest{TResponse}"/>.
    /// </summary>
    /// <param name="request">The untyped stream request instance.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of boxed response items.</returns>
    IAsyncEnumerable<object?> CreateStreamAsync(object request, CancellationToken cancellationToken = default);
}
