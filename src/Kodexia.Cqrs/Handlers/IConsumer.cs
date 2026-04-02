namespace Kodexia.Cqrs;

/// <summary>
/// Defines a consumer for a message that returns a response.
/// </summary>
public interface IConsumer<in TMessage, TResponse>
    where TMessage : IMessage<TResponse>
{
    Task<TResponse> ConsumeAsync(TMessage message, CancellationToken ct);
}

/// <summary>
/// Defines a consumer for a message that returns no response.
/// </summary>
public interface IConsumer<in TMessage>
    where TMessage : IMessage
{
    Task ConsumeAsync(TMessage message, CancellationToken ct);
}
