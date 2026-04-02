using Microsoft.Extensions.DependencyInjection;

namespace Kodexia.Cqrs.Internal;

internal abstract class MessageExecutorBase
{
    public abstract Task<object?> DeliverAsync(
        object message,
        IServiceProvider serviceProvider,
        CancellationToken ct);
}

internal abstract class MessageExecutor<TResponse> : MessageExecutorBase
{
    public abstract Task<TResponse> DeliverAsync(
        IMessage<TResponse> message,
        IServiceProvider serviceProvider,
        CancellationToken ct);
}

internal abstract class MessageExecutor : MessageExecutorBase
{
    public abstract Task<None> DeliverAsync(
        IMessage message,
        IServiceProvider serviceProvider,
        CancellationToken ct);
}

internal sealed class MessageExecutorImpl<TMessage, TResponse> : MessageExecutor<TResponse>
    where TMessage : IMessage<TResponse>
{
    public override async Task<object?> DeliverAsync(object message, IServiceProvider serviceProvider,
        CancellationToken ct)
        => await DeliverAsync((IMessage<TResponse>)message, serviceProvider, ct).ConfigureAwait(false);

    public override Task<TResponse> DeliverAsync(
        IMessage<TResponse> message,
        IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        var consumer = serviceProvider.GetRequiredService<IConsumer<TMessage, TResponse>>();
        
        var interceptors = serviceProvider.GetServices<IInterceptor<TMessage, TResponse>>();
        var chain = interceptors as IInterceptor<TMessage, TResponse>[] ?? [.. interceptors];

        if (chain.Length == 0)
        {
            return consumer.ConsumeAsync((TMessage)message, ct);
        }

        var executionChain = new HubExecutionChain<TMessage, TResponse>((TMessage)message, chain, consumer);
        return executionChain.NextAsync(ct);
    }
}

internal sealed class MessageExecutorImpl<TMessage> : MessageExecutor
    where TMessage : IMessage
{
    public override async Task<object?> DeliverAsync(object message, IServiceProvider serviceProvider,
        CancellationToken ct)
        => await DeliverAsync((IMessage)message, serviceProvider, ct).ConfigureAwait(false);

    public override Task<None> DeliverAsync(
        IMessage message,
        IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        async Task<None> ConsumerWrapper()
        {
            await serviceProvider
                .GetRequiredService<IConsumer<TMessage>>()
                .ConsumeAsync((TMessage)message, ct)
                .ConfigureAwait(false);
            return None.Value;
        }

        var interceptors = serviceProvider.GetServices<IInterceptor<TMessage, None>>();
        var chain = interceptors as IInterceptor<TMessage, None>[] ?? [.. interceptors];

        if (chain.Length == 0)
        {
            return ConsumerWrapper();
        }

        // We wrap the ConsumerWrapper task in a fake consumer to satisfy the chain's requirement
        var consumer = new VoidConsumerWrapper<TMessage>(ConsumerWrapper);
        var executionChain = new HubExecutionChain<TMessage, None>((TMessage)message, chain, consumer);
        return executionChain.NextAsync(ct);
    }
}

internal sealed class VoidConsumerWrapper<TMessage>(Func<Task<None>> callback) : IConsumer<TMessage, None>
    where TMessage : IMessage<None>
{
    public Task<None> ConsumeAsync(TMessage message, CancellationToken ct) => callback();
}
