namespace Kodexia.Cqrs;

/// <summary>
/// Represents a message that mutates system state.
/// </summary>
public interface IAction<out TResponse> : IMessage<TResponse>;

/// <summary>
/// Represents a message that mutates system state and returns no result.
/// </summary>
public interface IAction : IMessage;
