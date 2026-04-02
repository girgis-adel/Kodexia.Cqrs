using System.Runtime.CompilerServices;

namespace Kodexia.Cqrs.Tests.Dispatching;

public class StreamTests
{
    private static IHubExchange BuildExchange(Action<HubServiceConfiguration> configure)
    {
        var services = new ServiceCollection();
        services.AddKodexiaHub(configure);
        return services.BuildServiceProvider().GetRequiredService<IHubExchange>();
    }

    [Fact]
    public async Task OpenStreamAsync_Untyped_YieldsBoxedItems()
    {
        var exchange = BuildExchange(cfg =>
            cfg.RegisterHubClassesFromAssemblyContaining<StreamTests>());

        var items = new List<object?>();
        await foreach (var item in exchange.OpenStreamAsync((object)new NumberStreamSource(3)))
            items.Add(item);

        items.Should().BeEquivalentTo(new object[] { 1, 2, 3 });
    }

    [Fact]
    public async Task OpenStreamAsync_EmptyStream_YieldsNoItems()
    {
        var exchange = BuildExchange(cfg =>
            cfg.RegisterHubClassesFromAssemblyContaining<StreamTests>());

        var items = new List<int>();
        await foreach (var item in exchange.OpenStreamAsync(new NumberStreamSource(0)))
            items.Add(item);

        items.Should().BeEmpty();
    }

    [Fact]
    public void OpenStreamAsync_NullSource_ThrowsArgumentNullException()
    {
        var exchange = BuildExchange(cfg =>
            cfg.RegisterHubClassesFromAssemblyContaining<StreamTests>());

        var act = () => exchange.OpenStreamAsync((IStreamSource<int>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task OpenStreamAsync_WithInterceptor_ExecutesInterceptor()
    {
        var log = new List<string>();

        var services = new ServiceCollection();
        services.AddScoped(_ => log);
        services.AddKodexiaHub(cfg =>
        {
            cfg.RegisterHubClassesFromAssemblyContaining<StreamTests>();
            cfg.AddOpenStreamInterceptor(typeof(StreamLoggingInterceptor<,>));
        });

        var exchange = services.BuildServiceProvider().GetRequiredService<IHubExchange>();
        var items = new List<int>();
        await foreach (var item in exchange.OpenStreamAsync(new NumberStreamSource(2)))
            items.Add(item);

        items.Should().BeEquivalentTo([1, 2]);
        log.Should().Contain("stream-before");
    }
}

// --- Test Fixtures ---

public record NumberStreamSource(int Count) : IStreamSource<int>;
public class NumberStreamProvider : IProvider<NumberStreamSource, int>
{
    public async IAsyncEnumerable<int> ProvideAsync(NumberStreamSource source,
        [EnumeratorCancellation] CancellationToken ct)
    {
        for (var i = 1; i <= source.Count; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }
}

public class StreamLoggingInterceptor<TSource, TResponse>(List<string> log)
    : IStreamInterceptor<TSource, TResponse>
    where TSource : IStreamSource<TResponse>
{
    public async IAsyncEnumerable<TResponse> InterceptAsync(TSource source,
        StreamNextDelegate<TResponse> next,
        [EnumeratorCancellation] CancellationToken ct)
    {
        log.Add("stream-before");
        await foreach (var item in next().WithCancellation(ct))
            yield return item;
    }
}
