namespace Kodexia.Cqrs.Tests.Pipeline;

public class FallbackInterceptorTests
{
    [Fact]
    public async Task FallbackHandler_SuppressesException_ReturnsResponse()
    {
        var services = new ServiceCollection();
        services.AddKodexiaHub(cfg =>
            cfg.RegisterHubClassesFromAssemblyContaining<FallbackInterceptorTests>());

        var exchange = services.BuildServiceProvider().GetRequiredService<IHubExchange>();
        var result = await exchange.DeliverAsync(new FailingMessage("test"));

        result.Should().Be("fallback");
    }

    [Fact]
    public async Task ExceptionObserver_FiresAsSideEffect_DoesNotSuppressException()
    {
        var sideEffects = new List<string>();

        var services = new ServiceCollection();
        services.AddScoped(_ => sideEffects);
        services.AddKodexiaHub(cfg =>
            cfg.RegisterHubClassesFromAssemblyContaining<FallbackInterceptorTests>());

        var exchange = services.BuildServiceProvider().GetRequiredService<IHubExchange>();

        var act = async () => await exchange.DeliverAsync(new ActionOnlyFailingMessage());

        await act.Should().ThrowAsync<InvalidOperationException>();
        sideEffects.Should().Contain("observer-fired");
    }

    [Fact]
    public async Task FallbackHandler_WalksTypeHierarchy_BaseHandlerCatchesDerived()
    {
        var services = new ServiceCollection();
        services.AddKodexiaHub(cfg =>
            cfg.RegisterHubClassesFromAssemblyContaining<FallbackInterceptorTests>());

        var exchange = services.BuildServiceProvider().GetRequiredService<IHubExchange>();
        var result = await exchange.DeliverAsync(new HierarchyFailingMessage());

        result.Should().Be("base-handled");
    }

    [Fact]
    public async Task ApplyForAllExceptions_ObserversAlwaysFire()
    {
        var sideEffects = new List<string>();

        var services = new ServiceCollection();
        services.AddScoped(_ => sideEffects);
        services.AddKodexiaHub(cfg =>
        {
            cfg.RegisterHubClassesFromAssemblyContaining<FallbackInterceptorTests>();
            cfg.ExceptionObservationStrategy = ExceptionObservationStrategy.ApplyForAllExceptions;
        });

        var exchange = services.BuildServiceProvider().GetRequiredService<IHubExchange>();

        var result = await exchange.DeliverAsync(new FailingMessage("all-strategy"));

        result.Should().Be("fallback");
        sideEffects.Should().Contain("failing-observer");
    }
}

// --- Test Fixtures ---

public record FailingMessage(string Value) : IMessage<string>;
public class FailingMessageConsumer : IConsumer<FailingMessage, string>
{
    public Task<string> ConsumeAsync(FailingMessage message, CancellationToken ct)
        => throw new InvalidOperationException("Intentional failure");
}

public class FailingMessageFallbackHandler
    : IFallbackHandler<FailingMessage, string, InvalidOperationException>
{
    public Task HandleAsync(FailingMessage message, InvalidOperationException exception,
        FallbackState<string> state, CancellationToken ct)
    {
        state.SetHandled("fallback");
        return Task.CompletedTask;
    }
}

public class FailingMessageExceptionObserver(List<string> sideEffects)
    : IExceptionObserver<FailingMessage, InvalidOperationException>
{
    public Task ObserveAsync(FailingMessage message, InvalidOperationException exception, CancellationToken ct)
    {
        sideEffects.Add("failing-observer");
        return Task.CompletedTask;
    }
}

public record ActionOnlyFailingMessage : IMessage<string>;
public class ActionOnlyFailingMessageConsumer : IConsumer<ActionOnlyFailingMessage, string>
{
    public Task<string> ConsumeAsync(ActionOnlyFailingMessage message, CancellationToken ct)
        => throw new InvalidOperationException("Intentional");
}

public class ActionOnlyExceptionObserver(List<string> sideEffects)
    : IExceptionObserver<ActionOnlyFailingMessage, InvalidOperationException>
{
    public Task ObserveAsync(ActionOnlyFailingMessage message, InvalidOperationException exception, CancellationToken ct)
    {
        sideEffects.Add("observer-fired");
        return Task.CompletedTask;
    }
}

public record HierarchyFailingMessage : IMessage<string>;
public class HierarchyFailingMessageConsumer : IConsumer<HierarchyFailingMessage, string>
{
    public Task<string> ConsumeAsync(HierarchyFailingMessage message, CancellationToken ct)
        => throw new InvalidOperationException("Derived exception");
}

public class BaseFallbackHandler
    : IFallbackHandler<HierarchyFailingMessage, string, Exception>
{
    public Task HandleAsync(HierarchyFailingMessage message, Exception exception,
        FallbackState<string> state, CancellationToken ct)
    {
        state.SetHandled("base-handled");
        return Task.CompletedTask;
    }
}
