namespace Kodexia.Cqrs;

/// <summary>
/// Controls the order in which exception fallback handlers and observation interceptors are applied.
/// </summary>
public enum ExceptionObservationStrategy
{
    /// <summary>
    /// Exception observers run before fallback handlers.
    /// Actions run even if the exception is later suppressed by a fallback.
    /// </summary>
    ApplyForAllExceptions,

    /// <summary>
    /// Fallback handlers run before observers.
    /// Observers only run if the exception was NOT suppressed.
    /// This is the default strategy.
    /// </summary>
    ApplyForUnhandledExceptions
}
