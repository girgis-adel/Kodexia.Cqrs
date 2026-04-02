namespace Kodexia.Cqrs.Tests.Dispatching;

public class HubExchangeTests
{
    private static IHubExchange BuildExchange(Action<HubServiceConfiguration> configure)
    {
        var services = new ServiceCollection();
        services.AddKodexiaHub(configure);
        return services.BuildServiceProvider().GetRequiredService<IHubExchange>();
    }

    [Fact]
    public async Task DeliverAsync_WithTypedMessage_ReturnsConsumerResponse()
    {
        var exchange = BuildExchange(cfg =>
            cfg.RegisterHubClassesFromAssemblyContaining<HubExchangeTests>());

        var result = await exchange.DeliverAsync(new PingInquiry("hello"));

        result.Should().Be("hello-pong");
    }

    [Fact]
    public async Task DeliverAsync_WithVoidMessage_CompletesSuccessfully()
    {
        var exchange = BuildExchange(cfg =>
            cfg.RegisterHubClassesFromAssemblyContaining<HubExchangeTests>());

        var act = async () => await exchange.DeliverAsync(new NoopAction());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeliverAsync_Untyped_ReturnsBoxedResponse()
    {
        var exchange = BuildExchange(cfg =>
            cfg.RegisterHubClassesFromAssemblyContaining<HubExchangeTests>());

        var result = await exchange.DeliverAsync((object)new PingInquiry("boxed"));

        result.Should().Be("boxed-pong");
    }

    [Fact]
    public async Task DeliverAsync_WhenConsumerNotRegistered_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddKodexiaHub(cfg =>
            cfg.RegisterHubClassesFromAssembly(typeof(string).Assembly));
        var exchange = services.BuildServiceProvider().GetRequiredService<IHubExchange>();

        var act = async () => await exchange.DeliverAsync(new PingInquiry("test"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task OpenStreamAsync_YieldsAllItems()
    {
        var exchange = BuildExchange(cfg =>
            cfg.RegisterHubClassesFromAssemblyContaining<HubExchangeTests>());

        var items = new List<int>();
        await foreach (var item in exchange.OpenStreamAsync(new CountStreamSource(5)))
            items.Add(item);

        items.Should().BeEquivalentTo([1, 2, 3, 4, 5]);
    }

    [Fact]
    public async Task DeliverAsync_CalledMultipleTimes_UsesCache()
    {
        var exchange = BuildExchange(cfg =>
            cfg.RegisterHubClassesFromAssemblyContaining<HubExchangeTests>());

        for (var i = 0; i < 10; i++)
        {
            var result = await exchange.DeliverAsync(new PingInquiry("warm"));
            result.Should().Be("warm-pong");
        }
    }

    [Fact]
    public async Task DeliverAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        var exchange = BuildExchange(cfg =>
            cfg.RegisterHubClassesFromAssemblyContaining<HubExchangeTests>());

        var act = async () => await exchange.DeliverAsync((IMessage<string>)null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeliverAsync_Untyped_VoidMessage_ReturnsBoxedNone()
    {
        var exchange = BuildExchange(cfg =>
            cfg.RegisterHubClassesFromAssemblyContaining<HubExchangeTests>());

        var result = await exchange.DeliverAsync((object)new NoopAction());

        result.Should().Be(None.Value);
    }
}

// --- Test Fixtures ---

public record PingInquiry(string Message) : IInquiry<string>;
public class PingInquiryConsumer : IConsumer<PingInquiry, string>
{
    public Task<string> ConsumeAsync(PingInquiry message, CancellationToken ct)
        => Task.FromResult($"{message.Message}-pong");
}

public record NoopAction : IAction;
public class NoopActionConsumer : IConsumer<NoopAction>
{
    public Task ConsumeAsync(NoopAction message, CancellationToken ct) => Task.CompletedTask;
}

public record CountStreamSource(int Count) : IStreamSource<int>;
public class CountStreamProvider : IProvider<CountStreamSource, int>
{
    public async IAsyncEnumerable<int> ProvideAsync(CountStreamSource source,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        for (var i = 1; i <= source.Count; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }
}
