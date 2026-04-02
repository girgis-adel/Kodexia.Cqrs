namespace Kodexia.Cqrs.Tests.Pipeline;


public class PipelineOrderingTests
{
    private static ICqrsManager BuildManager(Action<CqrsManagerServiceConfiguration> configure)
    {
        var services = new ServiceCollection();
        services.AddKodexiaCqrs(configure);
        return services.BuildServiceProvider().GetRequiredService<ICqrsManager>();
    }

    [Fact]
    public async Task Pipeline_ExecutesBehaviorsInRegistrationOrder()
    {
        var log = new List<string>();

        var services = new ServiceCollection();
        services.AddScoped(_ => log);
        services.AddKodexiaCqrs(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<PipelineOrderingTests>();
            cfg.AddOpenBehavior(typeof(FirstBehavior<,>));
            cfg.AddOpenBehavior(typeof(SecondBehavior<,>));
        });

        var manager = services.BuildServiceProvider().GetRequiredService<ICqrsManager>();
        await manager.SendAsync(new OrderedRequest());

        log.Should().Equal("first-before", "second-before", "handler", "second-after", "first-after");
    }

    [Fact]
    public async Task Pipeline_WithNoBehaviors_CallsHandlerDirectly()
    {
        var manager = BuildManager(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<PipelineOrderingTests>());

        var result = await manager.SendAsync(new SimpleRequest("direct"));

        result.Should().Be("direct");
    }

    [Fact]
    public async Task Pipeline_CanShortCircuit_WithoutCallingHandler()
    {
        var services = new ServiceCollection();
        services.AddKodexiaCqrs(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<PipelineOrderingTests>();
            cfg.AddBehavior(typeof(IPipelineBehavior<ShortCircuitRequest, string>),
                            typeof(ShortCircuitBehavior));
        });

        var manager = services.BuildServiceProvider().GetRequiredService<ICqrsManager>();
        var result = await manager.SendAsync(new ShortCircuitRequest());

        result.Should().Be("short-circuited");
    }

    [Fact]
    public async Task PreProcessor_RunsBeforeHandler()
    {
        var log = new List<string>();

        var services = new ServiceCollection();
        services.AddScoped(_ => log);
        services.AddKodexiaCqrs(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<PipelineOrderingTests>();
            cfg.AddOpenRequestPreProcessor(typeof(LoggingPreProcessor<>));
        });

        var manager = services.BuildServiceProvider().GetRequiredService<ICqrsManager>();
        await manager.SendAsync(new SimpleRequest("test"));

        log.Should().Contain(x => x.StartsWith("pre:"));
    }


    [Fact]
    public async Task PostProcessor_RunsAfterHandler()
    {
        var log = new List<string>();

        var services = new ServiceCollection();
        services.AddScoped(_ => log);
        services.AddKodexiaCqrs(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<PipelineOrderingTests>();
            cfg.AddOpenRequestPostProcessor(typeof(LoggingPostProcessor<,>));
        });

        var manager = services.BuildServiceProvider().GetRequiredService<ICqrsManager>();
        await manager.SendAsync(new SimpleRequest("test"));

        log.Should().Contain(x => x.StartsWith("post:"));
    }

}

// ─── Test Fixtures ────────────────────────────────────────────────────────────

public record OrderedRequest : IRequest<Unit>;
public class OrderedRequestHandler(List<string> log) : IRequestHandler<OrderedRequest, Unit>
{
    public Task<Unit> HandleAsync(OrderedRequest request, CancellationToken ct)
    {
        log.Add("handler");
        return Unit.Task;
    }
}

public class FirstBehavior<TRequest, TResponse>(List<string> log)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        log.Add("first-before");
        var result = await next(ct);
        log.Add("first-after");
        return result;
    }
}

public class SecondBehavior<TRequest, TResponse>(List<string> log)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        log.Add("second-before");
        var result = await next(ct);
        log.Add("second-after");
        return result;
    }
}

public record SimpleRequest(string Value) : IRequest<string>;
public class SimpleRequestHandler : IRequestHandler<SimpleRequest, string>
{
    public Task<string> HandleAsync(SimpleRequest request, CancellationToken ct)
        => Task.FromResult(request.Value);
}

public record ShortCircuitRequest : IRequest<string>;
public class ShortCircuitRequestHandler : IRequestHandler<ShortCircuitRequest, string>
{
    // Should never be called — the behavior short-circuits
    public Task<string> HandleAsync(ShortCircuitRequest request, CancellationToken ct)
        => Task.FromResult("from-handler");
}

public class ShortCircuitBehavior : IPipelineBehavior<ShortCircuitRequest, string>
{
    public Task<string> HandleAsync(ShortCircuitRequest request, RequestHandlerDelegate<string> next, CancellationToken ct)
        => Task.FromResult("short-circuited"); // Never calls next
}

public class LoggingPreProcessor<TRequest>(List<string> log)
    : IRequestPreProcessor<TRequest>
    where TRequest : notnull
{
    public Task ProcessAsync(TRequest request, CancellationToken ct)
    {
        log.Add($"pre:{request}");
        return Task.CompletedTask;
    }
}

public class LoggingPostProcessor<TRequest, TResponse>(List<string> log)
    : IRequestPostProcessor<TRequest, TResponse>
    where TRequest : notnull
{
    public Task ProcessAsync(TRequest request, TResponse response, CancellationToken ct)
    {
        log.Add($"post:{request}");
        return Task.CompletedTask;
    }
}
