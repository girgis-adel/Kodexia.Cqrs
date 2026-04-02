namespace Kodexia.Cqrs;

/// <summary>
/// Defines a handler for a request that returns a response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled. Must implement <see cref="IRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of response produced by the handler.</typeparam>
/// <example>
/// <code>
/// public class GetUserHandler : IRequestHandler&lt;GetUserQuery, UserDto&gt;
/// {
///     public Task&lt;UserDto&gt; HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
///         => _repository.GetByIdAsync(request.UserId, cancellationToken);
/// }
/// </code>
/// </example>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the specified request and returns a response asynchronously.
    /// </summary>
    /// <param name="request">The incoming request instance.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous operation,
    /// containing the <typeparamref name="TResponse"/> result.
    /// </returns>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Defines a handler for a request that returns no response (void equivalent).
/// </summary>
/// <typeparam name="TRequest">The type of request being handled. Must implement <see cref="IRequest"/>.</typeparam>
/// <example>
/// <code>
/// public class DeleteUserHandler : IRequestHandler&lt;DeleteUserCommand&gt;
/// {
///     public Task HandleAsync(DeleteUserCommand request, CancellationToken cancellationToken)
///         => _repository.DeleteAsync(request.UserId, cancellationToken);
/// }
/// </code>
/// </example>
public interface IRequestHandler<in TRequest>
    where TRequest : IRequest
{
    /// <summary>
    /// Handles the specified request asynchronously.
    /// </summary>
    /// <param name="request">The incoming request instance.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    Task HandleAsync(TRequest request, CancellationToken cancellationToken);
}
