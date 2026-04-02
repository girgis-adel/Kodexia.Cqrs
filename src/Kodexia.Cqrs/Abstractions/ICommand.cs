namespace Kodexia.Cqrs;

/// <summary>
/// Semantic marker for requests that <strong>mutate state</strong> and return a response.
/// A command represents an intent to change something in the system.
/// </summary>
/// <typeparam name="TResponse">The type of the value returned after the command executes.</typeparam>
/// <remarks>
/// <para>
/// This is a semantic alias for <see cref="IRequest{TResponse}"/>. Handlers implement
/// <c>IRequestHandler&lt;TCommand, TResponse&gt;</c> exactly as with <see cref="IRequest{TResponse}"/>.
/// </para>
/// <para>
/// Use <see cref="ICommand"/> (non-generic) for commands that return no value.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public record CreateOrderCommand(Guid CustomerId, decimal Total) : ICommand&lt;Guid&gt;;
///
/// public class CreateOrderHandler : IRequestHandler&lt;CreateOrderCommand, Guid&gt;
/// {
///     public Task&lt;Guid&gt; HandleAsync(CreateOrderCommand request, CancellationToken ct)
///         => Task.FromResult(Guid.NewGuid());
/// }
/// </code>
/// </example>
public interface ICommand<out TResponse> : IRequest<TResponse>;

/// <summary>
/// Semantic marker for requests that <strong>mutate state</strong> and return no response.
/// A command represents an intent to change something in the system.
/// </summary>
/// <remarks>
/// This is a semantic alias for <see cref="IRequest"/>.
/// Handlers implement <c>IRequestHandler&lt;TCommand&gt;</c>.
/// </remarks>
/// <example>
/// <code>
/// public record ArchiveOrderCommand(Guid OrderId) : ICommand;
///
/// public class ArchiveOrderHandler : IRequestHandler&lt;ArchiveOrderCommand&gt;
/// {
///     public Task HandleAsync(ArchiveOrderCommand request, CancellationToken ct)
///         => Task.CompletedTask;
/// }
/// </code>
/// </example>
public interface ICommand : IRequest;
