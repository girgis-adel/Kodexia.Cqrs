namespace Kodexia.Cqrs;

/// <summary>
/// A built-in pipeline behavior that invokes all registered <see cref="IRequestPreProcessor{TRequest}"/>
/// implementations before passing the request to subsequent behaviors and the handler.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <remarks>
/// This behavior is automatically registered when pre-processors are added via
/// <c>CqrsManagerServiceConfiguration.AddRequestPreProcessor</c>.
/// </remarks>
public class RequestPreProcessorBehavior<TRequest, TResponse>(
    IEnumerable<IRequestPreProcessor<TRequest>> preProcessors)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IRequestPreProcessor<TRequest>> _preProcessors = preProcessors;

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        foreach (var processor in _preProcessors)
        {
            await processor.ProcessAsync(request, cancellationToken).ConfigureAwait(false);
        }

        return await next(cancellationToken).ConfigureAwait(false);
    }
}
