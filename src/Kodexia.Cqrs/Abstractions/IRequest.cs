namespace Kodexia.Cqrs;

/// <summary>
/// Marker interface for the base of all request types.
/// Do not implement this interface directly; use <see cref="IRequest{TResponse}"/> or <see cref="IRequest"/> instead.
/// </summary>
public interface IBaseRequest;

/// <summary>
/// Defines a request that returns a response of type <typeparamref name="TResponse"/>.
/// Implement this interface to create a query or a command that produces a result.
/// </summary>
/// <typeparam name="TResponse">The type of the value returned by the request handler.</typeparam>
/// <example>
/// <code>
/// public record GetUserByIdQuery(Guid UserId) : IRequest&lt;UserDto&gt;;
/// </code>
/// </example>
public interface IRequest<out TResponse> : IBaseRequest;

/// <summary>
/// Defines a request that returns no response (void equivalent).
/// Implement this interface to create a fire-and-forget command.
/// </summary>
/// <example>
/// <code>
/// public record DeleteUserCommand(Guid UserId) : IRequest;
/// </code>
/// </example>
public interface IRequest : IBaseRequest;
