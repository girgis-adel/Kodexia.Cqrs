namespace Kodexia.Cqrs;

/// <summary>
/// A unified interface for delivering messages and broadcasting signals.
/// </summary>
public interface IHubExchange : IDeliveryAgent, IBroadcastAgent;
