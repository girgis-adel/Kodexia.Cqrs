using Microsoft.Extensions.DependencyInjection;
using Kodexia.Cqrs.Internal;
using System.Collections.Concurrent;

namespace Kodexia.Cqrs;

/// <summary>
/// The default implementation of <see cref="IHubExchange"/>.
/// </summary>
public class HubExchange : IHubExchange
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBroadcastStrategy _strategy;

    private static readonly ConcurrentDictionary<Type, MessageExecutorBase>
        _messageExecutors = new();
    private static readonly ConcurrentDictionary<Type, SignalBroadcastExecutor>
        _signalExecutors = new();
    private static readonly ConcurrentDictionary<Type, StreamSourceExecutorBase>
        _streamExecutors = new();

    public HubExchange(IServiceProvider serviceProvider)
        : this(serviceProvider, new ForeachAwaitBroadcastStrategy()) { }

    public HubExchange(IServiceProvider serviceProvider, IBroadcastStrategy strategy)
    {
        _serviceProvider = serviceProvider;
        _strategy = strategy;
    }

    public Task<TResult> DeliverAsync<TResult>(IMessage<TResult> message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var executor = (MessageExecutor<TResult>)_messageExecutors.GetOrAdd(
            message.GetType(),
            static messageType =>
            {
                var type = typeof(MessageExecutorImpl<,>).MakeGenericType(messageType, typeof(TResult));
                return (MessageExecutorBase)Activator.CreateInstance(type)!;
            });

        return executor.DeliverAsync(message, _serviceProvider, ct);
    }

    public Task DeliverAsync<TMessage>(TMessage message, CancellationToken ct = default)
        where TMessage : IMessage
    {
        ArgumentNullException.ThrowIfNull(message);

        var executor = (MessageExecutor)_messageExecutors.GetOrAdd(
            message.GetType(),
            static messageType =>
            {
                var type = typeof(MessageExecutorImpl<>).MakeGenericType(messageType);
                return (MessageExecutorBase)Activator.CreateInstance(type)!;
            });

        return executor.DeliverAsync(message, _serviceProvider, ct);
    }

    public Task<object?> DeliverAsync(object message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var executor = _messageExecutors.GetOrAdd(message.GetType(), static messageType =>
        {
            var messageInterface = messageType.GetInterfaces()
                .FirstOrDefault(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessage<>));

            if (messageInterface is null)
            {
                messageInterface = messageType.GetInterfaces().FirstOrDefault(static i => i == typeof(IMessage))
                    ?? throw new ArgumentException($"Type '{messageType.Name}' is not a valid Hub message.", nameof(message));

                return (MessageExecutorBase)Activator.CreateInstance(typeof(MessageExecutorImpl<>).MakeGenericType(messageType))!;
            }

            var resultType = messageInterface.GetGenericArguments()[0];
            return (MessageExecutorBase)Activator.CreateInstance(typeof(MessageExecutorImpl<,>).MakeGenericType(messageType, resultType))!;
        });

        return executor.DeliverAsync(message, _serviceProvider, ct);
    }

    public IAsyncEnumerable<TResult> OpenStreamAsync<TResult>(IStreamSource<TResult> source, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var executor = (StreamSourceExecutor<TResult>)_streamExecutors.GetOrAdd(
            source.GetType(),
            static sourceType => (StreamSourceExecutorBase)Activator.CreateInstance(typeof(StreamSourceExecutorImpl<,>).MakeGenericType(sourceType, typeof(TResult)))!);

        return executor.OpenStreamAsync(source, _serviceProvider, ct);
    }

    public IAsyncEnumerable<object?> OpenStreamAsync(object source, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var executor = _streamExecutors.GetOrAdd(source.GetType(), static sourceType =>
        {
            var sourceInterface = sourceType.GetInterfaces()
                .FirstOrDefault(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamSource<>))
                ?? throw new ArgumentException($"Type '{sourceType.Name}' is not a valid Hub stream source.", nameof(source));

            var resultType = sourceInterface.GetGenericArguments()[0];
            return (StreamSourceExecutorBase)Activator.CreateInstance(typeof(StreamSourceExecutorImpl<,>).MakeGenericType(sourceType, resultType))!;
        });

        return executor.OpenStreamAsync(source, _serviceProvider, ct);
    }

    public Task BroadcastAsync(object signal, CancellationToken ct = default) =>
        signal switch
        {
            null => throw new ArgumentNullException(nameof(signal)),
            ISignal instance => BroadcastSignalAsync(instance, ct),
            _ => throw new ArgumentException("Object is not a valid Hub signal.", nameof(signal))
        };

    public Task BroadcastAsync<TSignal>(TSignal signal, CancellationToken ct = default)
        where TSignal : ISignal
    {
        ArgumentNullException.ThrowIfNull(signal);
        return BroadcastSignalAsync(signal, ct);
    }

    private Task BroadcastSignalAsync(ISignal signal, CancellationToken ct)
    {
        var executor = _signalExecutors.GetOrAdd(
            signal.GetType(),
            static signalType => (SignalBroadcastExecutor)Activator.CreateInstance(typeof(SignalBroadcastExecutorImpl<>).MakeGenericType(signalType))!);

        return executor.BroadcastAsync(signal, _serviceProvider, BroadcastCoreAsync, ct);
    }

    protected virtual Task BroadcastCoreAsync(
        IEnumerable<SubscriberExecutor> executors,
        ISignal signal,
        CancellationToken ct)
        => _strategy.BroadcastAsync(executors, signal, ct);
}
