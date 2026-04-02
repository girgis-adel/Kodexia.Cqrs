namespace Kodexia.Cqrs.Tests.Pipeline;

public class ExceptionHandlerTests
{
    [Fact]
    public async Task ExceptionHandler_SuppressesException_ReturnsResponse()
    {
        var services = new ServiceCollection();
        services.AddKodexiaCqrs(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<ExceptionHandlerTests>());

        var manager = services.BuildServiceProvider().GetRequiredService<ICqrsManager>();
        var result = await manager.SendAsync(new FailingRequest("test"));

        result.Should().Be("fallback");
    }

    [Fact]
    public async Task ExceptionAction_FiresAsSideEffect_DoesNotSuppressException()
    {
        var sideEffects = new List<string>();

        var services = new ServiceCollection();
        services.AddScoped(_ => sideEffects);
        services.AddKodexiaCqrs(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<ExceptionHandlerTests>());

        var manager = services.BuildServiceProvider().GetRequiredService<ICqrsManager>();

        var act = async () => await manager.SendAsync(new ActionOnlyFailingRequest());

        await act.Should().ThrowAsync<InvalidOperationException>();
        sideEffects.Should().Contain("action-fired");
    }

    [Fact]
    public async Task ExceptionHandler_WalksTypeHierarchy_BaseHandlerCatchesDerived()
    {
        var services = new ServiceCollection();
        services.AddKodexiaCqrs(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<ExceptionHandlerTests>());

        var manager = services.BuildServiceProvider().GetRequiredService<ICqrsManager>();
        var result = await manager.SendAsync(new HierarchyFailingRequest());

        // The base Exception handler should catch the derived InvalidOperationException
        result.Should().Be("base-handled");
    }

    [Fact]
    public async Task ApplyForAllExceptions_ActionsAlwaysFire()
    {
        var sideEffects = new List<string>();

        var services = new ServiceCollection();
        services.AddScoped(_ => sideEffects);
        services.AddKodexiaCqrs(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<ExceptionHandlerTests>();
            cfg.RequestExceptionActionProcessorStrategy =
                RequestExceptionActionProcessorStrategy.ApplyForAllExceptions;
        });

        var manager = services.BuildServiceProvider().GetRequiredService<ICqrsManager>();

        // The handler suppresses, but the action should still fire under ApplyForAllExceptions
        var result = await manager.SendAsync(new FailingRequest("all-strategy"));

        result.Should().Be("fallback");
        sideEffects.Should().Contain("failing-action");
    }
}

// ─── Test Fixtures ────────────────────────────────────────────────────────────

public record FailingRequest(string Value) : IRequest<string>;
public class FailingRequestHandler : IRequestHandler<FailingRequest, string>
{
    public Task<string> HandleAsync(FailingRequest request, CancellationToken ct)
        => throw new InvalidOperationException("Intentional failure");
}

public class FailingRequestExceptionHandler
    : IRequestExceptionHandler<FailingRequest, string, InvalidOperationException>
{
    public Task HandleAsync(FailingRequest request, InvalidOperationException exception,
        RequestExceptionHandlerState<string> state, CancellationToken ct)
    {
        state.SetHandled("fallback");
        return Task.CompletedTask;
    }
}

public class FailingRequestExceptionAction(List<string> sideEffects)
    : IRequestExceptionAction<FailingRequest, InvalidOperationException>
{
    public Task Execute(FailingRequest request, InvalidOperationException exception, CancellationToken ct)
    {
        sideEffects.Add("failing-action");
        return Task.CompletedTask;
    }
}

public record ActionOnlyFailingRequest : IRequest<string>;
public class ActionOnlyFailingRequestHandler : IRequestHandler<ActionOnlyFailingRequest, string>
{
    public Task<string> HandleAsync(ActionOnlyFailingRequest request, CancellationToken ct)
        => throw new InvalidOperationException("Intentional");
}

public class ActionOnlyExceptionAction(List<string> sideEffects)
    : IRequestExceptionAction<ActionOnlyFailingRequest, InvalidOperationException>
{
    public Task Execute(ActionOnlyFailingRequest request, InvalidOperationException exception, CancellationToken ct)
    {
        sideEffects.Add("action-fired");
        return Task.CompletedTask;
    }
}

public record HierarchyFailingRequest : IRequest<string>;
public class HierarchyFailingRequestHandler : IRequestHandler<HierarchyFailingRequest, string>
{
    public Task<string> HandleAsync(HierarchyFailingRequest request, CancellationToken ct)
        => throw new InvalidOperationException("Derived exception");
}

// Catches base Exception — should handle the derived InvalidOperationException
public class BaseExceptionHandler
    : IRequestExceptionHandler<HierarchyFailingRequest, string, Exception>
{
    public Task HandleAsync(HierarchyFailingRequest request, Exception exception,
        RequestExceptionHandlerState<string> state, CancellationToken ct)
    {
        state.SetHandled("base-handled");
        return Task.CompletedTask;
    }
}
