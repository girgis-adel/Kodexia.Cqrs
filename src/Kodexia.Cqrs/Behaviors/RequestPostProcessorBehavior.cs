namespace Kodexia.Cqrs;

/// <summary>
/// A built-in pipeline behavior that invokes all registered <see cref="IRequestPostProcessor{TRequest, TResponse}"/>
/// implementations after the handler and all subsequent behaviors have completed.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <remarks>
/// This behavior is automatically registered when post-processors are added via
/// <c>CqrsManagerServiceConfiguration.AddRequestPostProcessor</c>.
/// </remarks>
public class RequestPostProcessorBehavior<TRequest, TResponse>(
    IEnumerable<IRequestPostProcessor<TRequest, TResponse>> postProcessors)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IRequestPostProcessor<TRequest, TResponse>> _postProcessors = postProcessors;

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken).ConfigureAwait(false);

        foreach (var processor in _postProcessors)
        {
            await processor.ProcessAsync(request, response, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }
}
