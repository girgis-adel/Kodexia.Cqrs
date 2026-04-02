namespace Kodexia.Cqrs;

/// <summary>
/// Defines an interceptor that can process a message as it traverses the Hub.
/// </summary>
public interface IInterceptor<TMessage, TResponse>
    where TMessage : IMessage<TResponse>
{
    /// <summary>
    /// Intercepts the message and returns the response asynchronously.
    /// </summary>
    Task<TResponse> InterceptAsync(IMessageContext<TMessage, TResponse> context, CancellationToken ct);
}
