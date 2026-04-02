namespace Kodexia.Cqrs;

/// <summary>
/// Controls the order in which exception processors and exception action processors are applied
/// when both <see cref="IRequestExceptionHandler{TRequest,TResponse,TException}"/> and
/// <see cref="IRequestExceptionAction{TRequest,TException}"/> implementations are registered.
/// </summary>
public enum RequestExceptionActionProcessorStrategy
{
    /// <summary>
    /// Exception actions are applied first, then exception handlers.
    /// This means the action runs even if a handler will later suppress the exception.
    /// </summary>
    ApplyForAllExceptions,

    /// <summary>
    /// Exception handlers are applied first. Exception actions only run for exceptions
    /// not already marked as handled by an <see cref="IRequestExceptionHandler{TRequest,TResponse,TException}"/>.
    /// This is the <strong>default</strong> strategy.
    /// </summary>
    ApplyForUnhandledExceptions
}
