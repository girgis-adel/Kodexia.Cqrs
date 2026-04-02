namespace Kodexia.Cqrs.Internal;

/// <summary>
/// A built-in interceptor that invokes all registered <see cref="IPreConsumer{TMessage}"/>
/// implementations before passing the message down the chain.
/// </summary>
internal sealed class PreConsumptionInterceptor<TMessage, TResponse>(
    IEnumerable<IPreConsumer<TMessage>> processors)
    : IInterceptor<TMessage, TResponse>
    where TMessage : IMessage<TResponse>
{
    public async Task<TResponse> InterceptAsync(IMessageContext<TMessage, TResponse> context, CancellationToken ct)
    {
        foreach (var processor in processors)
        {
            await processor.ProcessAsync(context.Message, ct).ConfigureAwait(false);
        }

        return await context.NextAsync(ct).ConfigureAwait(false);
    }
}
