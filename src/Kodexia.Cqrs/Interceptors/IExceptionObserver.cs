namespace Kodexia.Cqrs;

/// <summary>
/// Defines an action to perform when an exception occurs during message processing.
/// Observers are side-effect only and cannot suppress exceptions.
/// </summary>
public interface IExceptionObserver<in TMessage, in TException>
    where TMessage : notnull
    where TException : Exception
{
    Task ObserveAsync(TMessage message, TException exception, CancellationToken ct);
}
