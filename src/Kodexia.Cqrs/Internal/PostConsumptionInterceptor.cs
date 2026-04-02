namespace Kodexia.Cqrs.Internal;

/// <summary>
/// A built-in interceptor that invokes all registered <see cref="IPostConsumer{TMessage, TResponse}"/>
/// implementations after message consumption.
/// </summary>
internal sealed class PostConsumptionInterceptor<TMessage, TResponse>(
    IEnumerable<IPostConsumer<TMessage, TResponse>> processors)
    : IInterceptor<TMessage, TResponse>
    where TMessage : IMessage<TResponse>
{
    public async Task<TResponse> InterceptAsync(IMessageContext<TMessage, TResponse> context, CancellationToken ct)
    {
        var response = await context.NextAsync(ct).ConfigureAwait(false);

        foreach (var processor in processors)
        {
            await processor.ProcessAsync(context.Message, response, ct).ConfigureAwait(false);
        }

        return response;
    }
}
