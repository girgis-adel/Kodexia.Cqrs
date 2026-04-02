using Microsoft.Extensions.DependencyInjection;

namespace Kodexia.Cqrs.Internal;

internal abstract class RequestHandlerBase
{
    public abstract Task<object?> HandleAsync(
        object request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

internal abstract class RequestHandlerWrapper<TResponse> : RequestHandlerBase
{
    public abstract Task<TResponse> HandleAsync(
        IRequest<TResponse> request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

internal abstract class RequestHandlerWrapper : RequestHandlerBase
{
    public abstract Task<Unit> HandleAsync(
        IRequest request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

internal sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    public override async Task<object?> HandleAsync(object request, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        => await HandleAsync((IRequest<TResponse>)request, serviceProvider, cancellationToken).ConfigureAwait(false);

    public override Task<TResponse> HandleAsync(
        IRequest<TResponse> request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        Task<TResponse> Handler() => serviceProvider
            .GetRequiredService<IRequestHandler<TRequest, TResponse>>()
            .HandleAsync((TRequest)request, cancellationToken);

        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>();
        var pipelines = behaviors as IPipelineBehavior<TRequest, TResponse>[]
            ?? [.. behaviors];

        if (pipelines.Length == 0)
            return Handler();

        var index = 0;

        Task<TResponse> Next(CancellationToken t)
        {
            if (index < pipelines.Length)
            {
                var behavior = pipelines[index++];
                return behavior.HandleAsync((TRequest)request, Next, t);
            }
            return Handler();
        }

        return Next(cancellationToken);
    }
}

internal sealed class RequestHandlerWrapperImpl<TRequest> : RequestHandlerWrapper
    where TRequest : IRequest
{
    public override async Task<object?> HandleAsync(object request, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        => await HandleAsync((IRequest)request, serviceProvider, cancellationToken).ConfigureAwait(false);

    public override Task<Unit> HandleAsync(
        IRequest request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        async Task<Unit> Handler()
        {
            await serviceProvider
                .GetRequiredService<IRequestHandler<TRequest>>()
                .HandleAsync((TRequest)request, cancellationToken)
                .ConfigureAwait(false);
            return Unit.Value;
        }

        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, Unit>>();
        var pipelines = behaviors as IPipelineBehavior<TRequest, Unit>[]
            ?? [.. behaviors];

        if (pipelines.Length == 0)
            return Handler();

        var index = 0;

        Task<Unit> Next(CancellationToken t)
        {
            if (index < pipelines.Length)
            {
                var behavior = pipelines[index++];
                return behavior.HandleAsync((TRequest)request, Next, t);
            }
            return Handler();
        }

        return Next(cancellationToken);
    }
}
