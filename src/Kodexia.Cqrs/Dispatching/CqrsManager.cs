using Microsoft.Extensions.DependencyInjection;
using Kodexia.Cqrs.Internal;

namespace Kodexia.Cqrs;

/// <summary>
/// The default implementation of <see cref="ICqrsManager"/>.
/// Dispatches requests through the registered pipeline behaviors and handlers.
/// Publishes notifications to all registered handlers via the configured <see cref="INotificationPublisher"/>.
/// </summary>
/// <remarks>
/// All wrapper types (request, notification, stream) are cached in <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>
/// instances keyed by the concrete request type. After the first dispatch for a given type,
/// all subsequent dispatches incur zero warming cost.
/// </remarks>
public class CqrsManager : ICqrsManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationPublisher _publisher;

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, RequestHandlerBase>
        _requestHandlers = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, NotificationHandlerWrapper>
        _notificationHandlers = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, StreamRequestHandlerBase>
        _streamRequestHandlers = new();

    /// <summary>
    /// Initializes a new instance of <see cref="CqrsManager"/> using the <see cref="ForeachAwaitPublisher"/> by default.
    /// </summary>
    /// <param name="serviceProvider">The application's service provider used to resolve handlers and behaviors.</param>
    public CqrsManager(IServiceProvider serviceProvider)
        : this(serviceProvider, new ForeachAwaitPublisher()) { }

    /// <summary>
    /// Initializes a new instance of <see cref="CqrsManager"/> with a custom notification publisher.
    /// </summary>
    /// <param name="serviceProvider">The application's service provider.</param>
    /// <param name="publisher">The strategy used to dispatch notifications to handlers.</param>
    public CqrsManager(IServiceProvider serviceProvider, INotificationPublisher publisher)
    {
        _serviceProvider = serviceProvider;
        _publisher = publisher;
    }

    /// <inheritdoc />
    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var handler = (RequestHandlerWrapper<TResponse>)_requestHandlers.GetOrAdd(
            request.GetType(),
            static requestType =>
            {
                var wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(requestType, typeof(TResponse));
                var wrapper = Activator.CreateInstance(wrapperType)
                    ?? throw new InvalidOperationException($"Could not create wrapper type for '{requestType.FullName}'.");
                return (RequestHandlerBase)wrapper;
            });

        return handler.HandleAsync(request, _serviceProvider, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        ArgumentNullException.ThrowIfNull(request);

        var handler = (RequestHandlerWrapper)_requestHandlers.GetOrAdd(
            request.GetType(),
            static requestType =>
            {
                var wrapperType = typeof(RequestHandlerWrapperImpl<>).MakeGenericType(requestType);
                var wrapper = Activator.CreateInstance(wrapperType)
                    ?? throw new InvalidOperationException($"Could not create wrapper type for '{requestType.FullName}'.");
                return (RequestHandlerBase)wrapper;
            });

        return handler.HandleAsync(request, _serviceProvider, cancellationToken);
    }

    /// <inheritdoc />
    public Task<object?> SendAsync(object request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var handler = _requestHandlers.GetOrAdd(request.GetType(), static requestType =>
        {
            Type wrapperType;

            var requestInterfaceType = requestType.GetInterfaces()
                .FirstOrDefault(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

            if (requestInterfaceType is null)
            {
                requestInterfaceType = requestType.GetInterfaces()
                    .FirstOrDefault(static i => i == typeof(IRequest))
                    ?? throw new ArgumentException(
                        $"The type '{requestType.Name}' does not implement {nameof(IRequest)} or {nameof(IRequest)}<TResponse>.",
                        nameof(request));

                wrapperType = typeof(RequestHandlerWrapperImpl<>).MakeGenericType(requestType);
            }
            else
            {
                var responseType = requestInterfaceType.GetGenericArguments()[0];
                wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(requestType, responseType);
            }

            var wrapper = Activator.CreateInstance(wrapperType)
                ?? throw new InvalidOperationException($"Could not create wrapper for type '{requestType.FullName}'.");
            return (RequestHandlerBase)wrapper;
        });

        return handler.HandleAsync(request, _serviceProvider, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TResponse> CreateStreamAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var streamHandler = (StreamRequestHandlerWrapper<TResponse>)_streamRequestHandlers.GetOrAdd(
            request.GetType(),
            static requestType =>
            {
                var wrapperType = typeof(StreamRequestHandlerWrapperImpl<,>).MakeGenericType(requestType, typeof(TResponse));
                var wrapper = Activator.CreateInstance(wrapperType)
                    ?? throw new InvalidOperationException($"Could not create wrapper for type '{requestType.FullName}'.");
                return (StreamRequestHandlerBase)wrapper;
            });

        return streamHandler.HandleAsync(request, _serviceProvider, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<object?> CreateStreamAsync(object request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var handler = _streamRequestHandlers.GetOrAdd(request.GetType(), static requestType =>
        {
            var requestInterfaceType = requestType.GetInterfaces()
                .FirstOrDefault(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamRequest<>))
                ?? throw new ArgumentException(
                    $"The type '{requestType.Name}' does not implement IStreamRequest<TResponse>.",
                    nameof(request));

            var responseType = requestInterfaceType.GetGenericArguments()[0];
            var wrapperType = typeof(StreamRequestHandlerWrapperImpl<,>).MakeGenericType(requestType, responseType);
            var wrapper = Activator.CreateInstance(wrapperType)
                ?? throw new InvalidOperationException($"Could not create wrapper for type '{requestType.FullName}'.");
            return (StreamRequestHandlerBase)wrapper;
        });

        return handler.HandleAsync(request, _serviceProvider, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync(object notification, CancellationToken cancellationToken = default) =>
        notification switch
        {
            null => throw new ArgumentNullException(nameof(notification)),
            INotification instance => PublishNotificationAsync(instance, cancellationToken),
            _ => throw new ArgumentException(
                $"The object does not implement {nameof(INotification)}.", nameof(notification))
        };

    /// <inheritdoc />
    public Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);
        return PublishNotificationAsync(notification, cancellationToken);
    }

    private Task PublishNotificationAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        var handler = _notificationHandlers.GetOrAdd(
            notification.GetType(),
            static notificationType =>
            {
                var wrapperType = typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(notificationType);
                var wrapper = Activator.CreateInstance(wrapperType)
                    ?? throw new InvalidOperationException($"Could not create wrapper for type '{notificationType.FullName}'.");
                return (NotificationHandlerWrapper)wrapper;
            });

        return handler.HandleAsync(notification, _serviceProvider, PublishCoreAsync, cancellationToken);
    }

    /// <summary>
    /// The core publish method that delegates to the configured <see cref="INotificationPublisher"/>.
    /// Can be overridden in derived classes to customize the publish behavior.
    /// </summary>
    protected virtual Task PublishCoreAsync(
        IEnumerable<NotificationHandlerExecutor> handlerExecutors,
        INotification notification,
        CancellationToken cancellationToken)
        => _publisher.PublishAsync(handlerExecutors, notification, cancellationToken);
}
