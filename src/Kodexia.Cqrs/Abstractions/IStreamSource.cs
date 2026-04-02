namespace Kodexia.Cqrs;

/// <summary>
/// A source of asynchronous data streams.
/// </summary>
/// <typeparam name="TResponse">The type of each item in the produced stream.</typeparam>
public interface IStreamSource<out TResponse>;
