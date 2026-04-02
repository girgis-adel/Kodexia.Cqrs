namespace Kodexia.Cqrs;

/// <summary>
/// A unified mediator interface that combines both <see cref="ISender"/> (request dispatching)
/// and <see cref="IPublisher"/> (notification publishing) into a single injectable surface.
/// </summary>
/// <remarks>
/// <para>
/// Inject <see cref="ICqrsManager"/> when a component needs both sending and publishing capabilities.
/// For components that only need one capability, prefer injecting the more focused
/// <see cref="ISender"/> or <see cref="IPublisher"/> interface directly — this follows the
/// Interface Segregation Principle and makes dependencies explicit.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Prefer focused interfaces where possible:
/// public class OrdersController(ISender sender) { ... }
/// public class EventRelay(IPublisher publisher) { ... }
///
/// // Use ICqrsManager when both are needed:
/// public class OrderService(ICqrsManager cqrs) { ... }
/// </code>
/// </example>
public interface ICqrsManager : ISender, IPublisher;
