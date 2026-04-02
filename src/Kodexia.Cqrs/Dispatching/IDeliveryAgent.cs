namespace Kodexia.Cqrs;

/// <summary>
/// Defines the ability to deliver messages and open streams through the Hub.
/// </summary>
public interface IDeliveryAgent
{
    /// <summary>
    /// Delivers a message and returns the response asynchronously.
    /// </summary>
    Task<TResult> DeliverAsync<TResult>(IMessage<TResult> message, CancellationToken ct = default);

    /// <summary>
    /// Delivers a message that returns no response.
    /// </summary>
    Task DeliverAsync<TMessage>(TMessage message, CancellationToken ct = default)
        where TMessage : IMessage;

    /// <summary>
    /// Delivers an untyped message.
    /// </summary>
    Task<object?> DeliverAsync(object message, CancellationToken ct = default);

    /// <summary>
    /// Opens an asynchronous stream from a source.
    /// </summary>
    IAsyncEnumerable<TResult> OpenStreamAsync<TResult>(IStreamSource<TResult> source, CancellationToken ct = default);

    /// <summary>
    /// Opens an asynchronous stream from an untyped source.
    /// </summary>
    IAsyncEnumerable<object?> OpenStreamAsync(object source, CancellationToken ct = default);
}
