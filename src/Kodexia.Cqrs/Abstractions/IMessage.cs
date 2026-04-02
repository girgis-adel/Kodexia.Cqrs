namespace Kodexia.Cqrs;

/// <summary>
/// Root marker interface for all messages traversing the Hub.
/// </summary>
public interface IHubMessage;

/// <summary>
/// Defines a message that expects a response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">The type of the result produced by the message consumer.</typeparam>
public interface IMessage<out TResponse> : IHubMessage;

/// <summary>
/// Defines a message that returns no response (fire-and-forget).
/// </summary>
public interface IMessage : IMessage<None>;
