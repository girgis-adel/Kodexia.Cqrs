namespace Kodexia.Cqrs;

/// <summary>
/// Carries the state of exception handling within a <see cref="IRequestExceptionHandler{TRequest, TResponse, TException}"/>.
/// Use <see cref="SetHandled"/> to mark the exception as handled and optionally supply a fallback response.
/// </summary>
/// <typeparam name="TResponse">The type of the response produced by the request handler.</typeparam>
public class RequestExceptionHandlerState<TResponse>
{
    /// <summary>
    /// Gets a value indicating whether the exception has been marked as handled.
    /// When <see langword="true"/>, exception propagation stops and <see cref="Response"/> is returned.
    /// </summary>
    public bool Handled { get; private set; }

    /// <summary>
    /// Gets the fallback response to return when the exception has been handled.
    /// Returns <see langword="null"/> if <see cref="Handled"/> is <see langword="false"/> or no response was provided.
    /// </summary>
    public TResponse? Response { get; private set; }

    /// <summary>
    /// Marks the exception as handled and sets the fallback <paramref name="response"/>.
    /// After calling this method, the exception will not propagate to the caller.
    /// </summary>
    /// <param name="response">The response to return to the caller in place of the exception.</param>
    public void SetHandled(TResponse response)
    {
        Response = response;
        Handled = true;
    }
}
