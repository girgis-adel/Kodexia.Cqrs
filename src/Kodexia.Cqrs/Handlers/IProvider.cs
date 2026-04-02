namespace Kodexia.Cqrs;

/// <summary>
/// Defines a provider for a stream source.
/// </summary>
public interface IProvider<in TSource, out TResponse>
    where TSource : IStreamSource<TResponse>
{
    IAsyncEnumerable<TResponse> ProvideAsync(TSource source, CancellationToken ct);
}
