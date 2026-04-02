namespace Kodexia.Cqrs;

/// <summary>
/// Provides context for a message as it traverses the Hub's interceptor chain.
/// </summary>
public interface IMessageContext<out TMessage, TResponse>
    where TMessage : IMessage<TResponse>
{
    /// <summary>
    /// Gets the message being processed.
    /// </summary>
    TMessage Message { get; }

    /// <summary>
    /// Dispatches the message to the next interceptor in the chain, or to the consumer.
    /// </summary>
    Task<TResponse> NextAsync(CancellationToken ct);
}
