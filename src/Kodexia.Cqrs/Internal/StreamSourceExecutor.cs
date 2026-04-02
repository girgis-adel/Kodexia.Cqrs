using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace Kodexia.Cqrs.Internal;

internal abstract class StreamSourceExecutorBase
{
    public abstract IAsyncEnumerable<object?> OpenStreamAsync(
        object source,
        IServiceProvider serviceProvider,
        CancellationToken ct);
}

internal abstract class StreamSourceExecutor<TResponse> : StreamSourceExecutorBase
{
    public abstract IAsyncEnumerable<TResponse> OpenStreamAsync(
        IStreamSource<TResponse> source,
        IServiceProvider serviceProvider,
        CancellationToken ct);
}

internal sealed class StreamSourceExecutorImpl<TSource, TResponse>
    : StreamSourceExecutor<TResponse>
    where TSource : IStreamSource<TResponse>
{
    public override async IAsyncEnumerable<object?> OpenStreamAsync(
        object source,
        IServiceProvider serviceProvider,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var item in OpenStreamAsync((IStreamSource<TResponse>)source, serviceProvider, ct))
        {
            yield return item;
        }
    }

    public override async IAsyncEnumerable<TResponse> OpenStreamAsync(
        IStreamSource<TResponse> source,
        IServiceProvider serviceProvider,
        [EnumeratorCancellation] CancellationToken ct)
    {
        IAsyncEnumerable<TResponse> FinalSource() => serviceProvider
            .GetRequiredService<IProvider<TSource, TResponse>>()
            .ProvideAsync((TSource)source, ct);

        var interceptors = serviceProvider.GetServices<IStreamInterceptor<TSource, TResponse>>();
        var chain = interceptors as IStreamInterceptor<TSource, TResponse>[] ?? [.. interceptors];

        if (chain.Length == 0)
        {
            await foreach (var item in FinalSource().WithCancellation(ct))
            {
                yield return item;
            }
            yield break;
        }

        var index = 0;

        IAsyncEnumerable<TResponse> Next()
        {
            if (index < chain.Length)
            {
                var interceptor = chain[index++];
                return interceptor.InterceptAsync((TSource)source, Next, ct);
            }
            return FinalSource();
        }

        await foreach (var item in Next().WithCancellation(ct))
        {
            yield return item;
        }
    }
}
