namespace Kodexia.Cqrs;

/// <summary>
/// Defines a fallback handler for exceptions thrown during message processing.
/// </summary>
public interface IFallbackHandler<in TMessage, TResponse, in TException>
    where TMessage : notnull
    where TException : Exception
{
    Task HandleAsync(
        TMessage message,
        TException exception,
        FallbackState<TResponse> state,
        CancellationToken ct);
}
