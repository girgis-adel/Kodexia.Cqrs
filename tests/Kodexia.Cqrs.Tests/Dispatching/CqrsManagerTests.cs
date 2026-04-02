namespace Kodexia.Cqrs.Tests.Dispatching;


public class CqrsManagerTests
{
    private static ICqrsManager BuildManager(Action<CqrsManagerServiceConfiguration> configure)
    {
        var services = new ServiceCollection();
        services.AddKodexiaCqrs(configure);
        return services.BuildServiceProvider().GetRequiredService<ICqrsManager>();
    }

    // ─── Basic Request / Response ────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_WithTypedRequest_ReturnsHandlerResponse()
    {
        var manager = BuildManager(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<CqrsManagerTests>());

        var result = await manager.SendAsync(new PingQuery("hello"));

        result.Should().Be("hello-pong");
    }

    [Fact]
    public async Task SendAsync_WithVoidRequest_CompletesSuccessfully()
    {
        var manager = BuildManager(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<CqrsManagerTests>());

        var act = async () => await manager.SendAsync<NoopCommand>(new NoopCommand());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_Untyped_ReturnsBoxedResponse()
    {
        var manager = BuildManager(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<CqrsManagerTests>());

        var result = await manager.SendAsync((object)new PingQuery("boxed"));

        result.Should().Be("boxed-pong");
    }

    [Fact]
    public async Task SendAsync_WhenHandlerNotRegistered_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddKodexiaCqrs(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(string).Assembly)); // empty assembly
        var manager = services.BuildServiceProvider().GetRequiredService<ICqrsManager>();

        var act = async () => await manager.SendAsync(new PingQuery("test"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ─── Semantic Interfaces ────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_ICommand_WorksIdenticallyToIRequest()
    {
        var manager = BuildManager(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<CqrsManagerTests>());

        var id = await manager.SendAsync(new CreateItemCommand("my-item"));

        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SendAsync_IQuery_WorksIdenticallyToIRequest()
    {
        var manager = BuildManager(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<CqrsManagerTests>());

        var result = await manager.SendAsync(new GetItemQuery(Guid.NewGuid()));

        result.Should().NotBeNull();
    }

    // ─── Streaming ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateStreamAsync_YieldsAllItems()
    {
        var manager = BuildManager(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<CqrsManagerTests>());

        var items = new List<int>();
        await foreach (var item in manager.CreateStreamAsync(new CountStreamRequest(5)))
            items.Add(item);

        items.Should().BeEquivalentTo([1, 2, 3, 4, 5]);
    }

    // ─── Handler Cache ───────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_CalledMultipleTimes_SameType_DoesNotThrow()
    {
        var manager = BuildManager(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<CqrsManagerTests>());

        for (var i = 0; i < 10; i++)
        {
            var result = await manager.SendAsync(new PingQuery("warm"));
            result.Should().Be("warm-pong");
        }
    }
}

// ─── Test Fixtures ────────────────────────────────────────────────────────────

public record PingQuery(string Message) : IQuery<string>;
public class PingQueryHandler : IRequestHandler<PingQuery, string>
{
    public Task<string> HandleAsync(PingQuery request, CancellationToken ct)
        => Task.FromResult($"{request.Message}-pong");
}

public record NoopCommand : IRequest;
public class NoopCommandHandler : IRequestHandler<NoopCommand>
{
    public Task HandleAsync(NoopCommand request, CancellationToken ct) => Task.CompletedTask;
}

public record CreateItemCommand(string Name) : ICommand<Guid>;
public class CreateItemCommandHandler : IRequestHandler<CreateItemCommand, Guid>
{
    public Task<Guid> HandleAsync(CreateItemCommand request, CancellationToken ct)
        => Task.FromResult(Guid.NewGuid());
}

public record GetItemQuery(Guid Id) : IQuery<string>;
public class GetItemQueryHandler : IRequestHandler<GetItemQuery, string>
{
    public Task<string> HandleAsync(GetItemQuery request, CancellationToken ct)
        => Task.FromResult($"item-{request.Id}");
}

public record CountStreamRequest(int Count) : IStreamRequest<int>;
public class CountStreamHandler : IStreamRequestHandler<CountStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(CountStreamRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        for (var i = 1; i <= request.Count; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }
}
