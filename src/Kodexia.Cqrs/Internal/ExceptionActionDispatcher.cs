using Microsoft.Extensions.DependencyInjection;

namespace Kodexia.Cqrs.Internal;

/// <summary>
/// Strongly-typed cached dispatcher for <see cref="IRequestExceptionAction{TRequest, TException}"/>.
/// </summary>
internal abstract class ExceptionActionDispatcher<TRequest>
    where TRequest : notnull
{
    public abstract Task DispatchAsync(
        IServiceProvider serviceProvider,
        TRequest request,
        Exception exception,
        CancellationToken cancellationToken);
}

internal sealed class ExceptionActionDispatcher<TRequest, TException>
    : ExceptionActionDispatcher<TRequest>
    where TRequest : notnull
    where TException : Exception
{
    public override async Task DispatchAsync(
        IServiceProvider serviceProvider,
        TRequest request,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var actions = serviceProvider.GetServices<IRequestExceptionAction<TRequest, TException>>();
        var actionList = actions as IRequestExceptionAction<TRequest, TException>[]
            ?? [.. actions];

        if (actionList.Length == 0)
            return;

        var orderedActions = HandlersOrderer.Prioritize<TRequest>(
            actionList.Cast<object>().ToList(), request);

        foreach (var actionObj in orderedActions)
        {
            var action = (IRequestExceptionAction<TRequest, TException>)actionObj;
            await action.Execute(request, (TException)exception, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}

internal static class ExceptionActionDispatcherFactory
{
    public static ExceptionActionDispatcher<TRequest> Create<TRequest>(Type exceptionType)
        where TRequest : notnull
    {
        var dispatcherType = typeof(ExceptionActionDispatcher<,>)
            .MakeGenericType(typeof(TRequest), exceptionType);

        return (ExceptionActionDispatcher<TRequest>)(
            Activator.CreateInstance(dispatcherType)
            ?? throw new InvalidOperationException(
                $"Could not create exception action dispatcher for exception type '{exceptionType.FullName}'."));
    }
}
