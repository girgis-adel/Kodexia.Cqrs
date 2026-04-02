using System.Runtime.CompilerServices;

namespace Kodexia.Cqrs.Tests.Dispatching;

public class StreamTests
{
    private static ICqrsManager BuildManager(Action<CqrsManagerServiceConfiguration> configure)
    {
        var services = new ServiceCollection();
        services.AddKodexiaCqrs(configure);
        return services.BuildServiceProvider().GetRequiredService<ICqrsManager>();
    }

    [Fact]
    public async Task CreateStreamAsync_Untyped_YieldsBoxedItems()
    {
        var manager = BuildManager(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<StreamTests>());

        var items = new List<object?>();
        await foreach (var item in manager.CreateStreamAsync((object)new NumberStreamRequest(3)))
            items.Add(item);

        items.Should().BeEquivalentTo(new object[] { 1, 2, 3 });
    }

    [Fact]
    public async Task CreateStreamAsync_EmptyStream_YieldsNoItems()
    {
        var manager = BuildManager(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<StreamTests>());

        var items = new List<int>();
        await foreach (var item in manager.CreateStreamAsync(new NumberStreamRequest(0)))
            items.Add(item);

        items.Should().BeEmpty();
    }

    [Fact]
    public void CreateStreamAsync_NullRequest_ThrowsArgumentNullException()
    {
        var manager = BuildManager(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<StreamTests>());

        var act = () => manager.CreateStreamAsync((IStreamRequest<int>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateStreamAsync_WithBehavior_ExecutesBehavior()
    {
        var log = new List<string>();

        var services = new ServiceCollection();
        services.AddScoped(_ => log);
        services.AddKodexiaCqrs(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<StreamTests>();
            cfg.AddOpenStreamBehavior(typeof(StreamLoggingBehavior<,>));
        });

        var manager = services.BuildServiceProvider().GetRequiredService<ICqrsManager>();
        var items = new List<int>();
        await foreach (var item in manager.CreateStreamAsync(new NumberStreamRequest(2)))
            items.Add(item);

        items.Should().BeEquivalentTo([1, 2]);
        log.Should().Contain("stream-before");
    }
}

// ─── Test Fixtures ────────────────────────────────────────────────────────────

public record NumberStreamRequest(int Count) : IStreamRequest<int>;
public class NumberStreamHandler : IStreamRequestHandler<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(NumberStreamRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        for (var i = 1; i <= request.Count; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }
}

public class StreamLoggingBehavior<TRequest, TResponse>(List<string> log)
    : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async IAsyncEnumerable<TResponse> HandleAsync(TRequest request,
        StreamHandlerDelegate<TResponse> next,
        [EnumeratorCancellation] CancellationToken ct)
    {
        log.Add("stream-before");
        await foreach (var item in next().WithCancellation(ct))
            yield return item;
    }
}
