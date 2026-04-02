namespace Kodexia.Cqrs;

/// <summary>
/// Represents a message that only reads system state.
/// </summary>
public interface IInquiry<out TResponse> : IMessage<TResponse>;
