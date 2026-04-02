namespace Kodexia.Cqrs.Internal;

/// <summary>
/// A linear execution chain that iterates through interceptors before calling the consumer.
/// Implements IMessageContext to provide state to each interceptor.
/// </summary>
internal sealed class HubExecutionChain<TMessage, TResponse> : IMessageContext<TMessage, TResponse>
    where TMessage : IMessage<TResponse>
{
    private readonly IInterceptor<TMessage, TResponse>[] _interceptors;
    private readonly IConsumer<TMessage, TResponse> _consumer;
    private int _index = -1;

    public TMessage Message { get; }

    public HubExecutionChain(
        TMessage message,
        IInterceptor<TMessage, TResponse>[] interceptors,
        IConsumer<TMessage, TResponse> consumer)
    {
        Message = message;
        _interceptors = interceptors;
        _consumer = consumer;
    }

    public Task<TResponse> NextAsync(CancellationToken ct)
    {
        _index++;

        if (_index < _interceptors.Length)
        {
            return _interceptors[_index].InterceptAsync(this, ct);
        }

        return _consumer.ConsumeAsync(Message, ct);
    }
}
