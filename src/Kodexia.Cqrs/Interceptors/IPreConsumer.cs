namespace Kodexia.Cqrs;

/// <summary>
/// Defines a task to be executed BEFORE a message is consumed.
/// </summary>
public interface IPreConsumer<in TMessage>
    where TMessage : IHubMessage
{
    Task ProcessAsync(TMessage message, CancellationToken ct);
}
