namespace Kodexia.Cqrs;

/// <summary>
/// Semantic marker for requests that <strong>read state</strong> and return a response.
/// A query represents an intent to retrieve information without causing side effects.
/// </summary>
/// <typeparam name="TResponse">The type of the data returned by the query.</typeparam>
/// <remarks>
/// <para>
/// This is a semantic alias for <see cref="IRequest{TResponse}"/>. Handlers implement
/// <c>IRequestHandler&lt;TQuery, TResponse&gt;</c> exactly as with <see cref="IRequest{TResponse}"/>.
/// </para>
/// <para>
/// A query should be a pure read operation — it should not modify system state.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public record GetOrderByIdQuery(Guid OrderId) : IQuery&lt;OrderDto&gt;;
///
/// public class GetOrderByIdHandler : IRequestHandler&lt;GetOrderByIdQuery, OrderDto&gt;
/// {
///     public Task&lt;OrderDto&gt; HandleAsync(GetOrderByIdQuery request, CancellationToken ct)
///         => Task.FromResult(new OrderDto(request.OrderId));
/// }
/// </code>
/// </example>
public interface IQuery<out TResponse> : IRequest<TResponse>;
