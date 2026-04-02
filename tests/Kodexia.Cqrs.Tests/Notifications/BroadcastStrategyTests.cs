namespace Kodexia.Cqrs.Tests.Notifications;

public class BroadcastStrategyTests
{
    private static IHubExchange BuildExchange(Action<HubServiceConfiguration> configure)
    {
        var services = new ServiceCollection();
        services.AddKodexiaHub(configure);
        return services.BuildServiceProvider().GetRequiredService<IHubExchange>();
    }

    [Fact]
    public async Task BroadcastAsync_InvokesAllSubscribers()
    {
        var results = new List<string>();

        var services = new ServiceCollection();
        services.AddScoped(_ => results);
        services.AddKodexiaHub(cfg =>
            cfg.RegisterHubClassesFromAssemblyContaining<BroadcastStrategyTests>());

        var exchange = services.BuildServiceProvider().GetRequiredService<IHubExchange>();
        await exchange.BroadcastAsync(new SampleSignal("test"));

        results.Should().Contain("A:test").And.Contain("B:test");
    }

    [Fact]
    public async Task BroadcastAsync_WithNoSubscribers_DoesNotThrow()
    {
        var exchange = BuildExchange(cfg =>
            cfg.RegisterHubClassesFromAssembly(typeof(string).Assembly));

        var act = async () => await exchange.BroadcastAsync(new SampleSignal("empty"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task BroadcastAsync_DeduplicatesSubscribers_WhenSameTypeRegisteredTwice()
    {
        var count = 0;

        var services = new ServiceCollection();
        services.AddKodexiaHub(cfg =>
            cfg.RegisterHubClassesFromAssembly(typeof(string).Assembly));
        
        services.AddTransient<ISubscriber<CountSignal>>(_ => new CountSubscriber(() => count++));
        services.AddTransient<ISubscriber<CountSignal>>(_ => new CountSubscriber(() => count++));

        var exchange = services.BuildServiceProvider().GetRequiredService<IHubExchange>();
        await exchange.BroadcastAsync(new CountSignal());

        count.Should().Be(1);
    }

    [Fact]
    public async Task BroadcastAsync_WithTaskWhenAllStrategy_InvokesAllConcurrently()
    {
        var bag = new System.Collections.Concurrent.ConcurrentBag<string>();

        var services = new ServiceCollection();
        services.AddScoped(_ => bag);
        services.AddKodexiaHub(cfg =>
        {
            cfg.RegisterHubClassesFromAssemblyContaining<BroadcastStrategyTests>();
            cfg.BroadcastStrategyType = typeof(TaskWhenAllBroadcastStrategy);
        });

        var exchange = services.BuildServiceProvider().GetRequiredService<IHubExchange>();
        await exchange.BroadcastAsync(new ConcurrentSignal());

        bag.Should().HaveCount(2);
    }
}

// --- Test Fixtures ---

public record SampleSignal(string Value) : ISignal;

public class SampleSubscriberA(List<string> results) : ISubscriber<SampleSignal>
{
    public Task OnSignalAsync(SampleSignal signal, CancellationToken ct)
    {
        results.Add($"A:{signal.Value}");
        return Task.CompletedTask;
    }
}

public class SampleSubscriberB(List<string> results) : ISubscriber<SampleSignal>
{
    public Task OnSignalAsync(SampleSignal signal, CancellationToken ct)
    {
        results.Add($"B:{signal.Value}");
        return Task.CompletedTask;
    }
}

public record CountSignal : ISignal;
public class CountSubscriber(Action increment) : ISubscriber<CountSignal>
{
    public Task OnSignalAsync(CountSignal signal, CancellationToken ct)
    {
        increment();
        return Task.CompletedTask;
    }
}

public record ConcurrentSignal : ISignal;

public class ConcurrentSubscriberA(System.Collections.Concurrent.ConcurrentBag<string> bag)
    : ISubscriber<ConcurrentSignal>
{
    public Task OnSignalAsync(ConcurrentSignal signal, CancellationToken ct)
    {
        bag.Add("A");
        return Task.CompletedTask;
    }
}

public class ConcurrentSubscriberB(System.Collections.Concurrent.ConcurrentBag<string> bag)
    : ISubscriber<ConcurrentSignal>
{
    public Task OnSignalAsync(ConcurrentSignal signal, CancellationToken ct)
    {
        bag.Add("B");
        return Task.CompletedTask;
    }
}
