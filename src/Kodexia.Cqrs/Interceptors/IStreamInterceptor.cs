namespace Kodexia.Cqrs;

/// <summary>
/// A delegate that represents the next step in a stream execution pipeline.
/// </summary>
public delegate IAsyncEnumerable<TResponse> StreamNextDelegate<out TResponse>();

/// <summary>
/// Defines an interceptor for an asynchronous stream source.
/// </summary>
public interface IStreamInterceptor<in TSource, TResponse>
    where TSource : IStreamSource<TResponse>
{
    IAsyncEnumerable<TResponse> InterceptAsync(TSource source, StreamNextDelegate<TResponse> next, CancellationToken ct);
}
