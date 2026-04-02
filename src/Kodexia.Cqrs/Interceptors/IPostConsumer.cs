namespace Kodexia.Cqrs;

/// <summary>
/// Defines a task to be executed AFTER a message is consumed.
/// </summary>
public interface IPostConsumer<in TMessage, in TResponse>
    where TMessage : IMessage<TResponse>
{
    Task ProcessAsync(TMessage message, TResponse response, CancellationToken ct);
}
