namespace Kodexia.Cqrs;

/// <summary>
/// Represents the state of a fallback operation for an exception.
/// </summary>
public class FallbackState<TResponse>
{
    /// <summary>
    /// Gets or sets whether the exception has been handled.
    /// </summary>
    public bool Handled { get; private set; }

    /// <summary>
    /// Gets the fallback response if the exception was handled.
    /// </summary>
    public TResponse? Response { get; private set; }

    /// <summary>
    /// Marks the exception as handled and provides a fallback response.
    /// </summary>
    public void SetHandled(TResponse response)
    {
        Handled = true;
        Response = response;
    }
}
