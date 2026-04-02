using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace Kodexia.Cqrs.Internal;

internal abstract class StreamRequestHandlerBase
{
    public abstract IAsyncEnumerable<object?> HandleAsync(
        object request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

internal abstract class StreamRequestHandlerWrapper<TResponse> : StreamRequestHandlerBase
{
    public abstract IAsyncEnumerable<TResponse> HandleAsync(
        IStreamRequest<TResponse> request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

internal sealed class StreamRequestHandlerWrapperImpl<TRequest, TResponse>
    : StreamRequestHandlerWrapper<TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    public override async IAsyncEnumerable<object?> HandleAsync(
        object request,
        IServiceProvider serviceProvider,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in HandleAsync(
            (IStreamRequest<TResponse>)request, serviceProvider, cancellationToken))
        {
            yield return item;
        }
    }

    public override async IAsyncEnumerable<TResponse> HandleAsync(
        IStreamRequest<TResponse> request,
        IServiceProvider serviceProvider,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IAsyncEnumerable<TResponse> Handler() => serviceProvider
            .GetRequiredService<IStreamRequestHandler<TRequest, TResponse>>()
            .HandleAsync((TRequest)request, cancellationToken);

        var behaviors = serviceProvider.GetServices<IStreamPipelineBehavior<TRequest, TResponse>>();
        var pipelines = behaviors as IStreamPipelineBehavior<TRequest, TResponse>[]
            ?? [.. behaviors];

        IAsyncEnumerable<TResponse> items;

        if (pipelines.Length == 0)
        {
            items = Handler();
        }
        else
        {
            var index = 0;

            IAsyncEnumerable<TResponse> Next()
            {
                if (index < pipelines.Length)
                {
                    var behavior = pipelines[index++];
                    return behavior.HandleAsync((TRequest)request, Next, cancellationToken);
                }
                return Handler();
            }

            items = Next();
        }

        await foreach (var item in items.WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }
}
