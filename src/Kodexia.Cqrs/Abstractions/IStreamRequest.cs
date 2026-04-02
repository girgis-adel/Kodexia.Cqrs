namespace Kodexia.Cqrs;

/// <summary>
/// Marker interface for a streaming request that asynchronously yields multiple responses
/// of type <typeparamref name="TResponse"/> via <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
/// <typeparam name="TResponse">The type of each yielded item in the stream.</typeparam>
/// <remarks>
/// Stream requests are dispatched via <see cref="ISender.CreateStreamAsync{TResponse}"/>
/// and handled by <c>IStreamRequestHandler&lt;TRequest, TResponse&gt;</c>.
/// Use streaming for large datasets, server-sent events, or real-time data feeds.
/// </remarks>
/// <example>
/// <code>
/// public record GetProductsStreamRequest(string Category) : IStreamRequest&lt;ProductDto&gt;;
/// </code>
/// </example>
public interface IStreamRequest<out TResponse>;
