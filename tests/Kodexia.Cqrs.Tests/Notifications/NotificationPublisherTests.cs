namespace Kodexia.Cqrs.Tests.Notifications;


public class NotificationPublisherTests
{
    private static ICqrsManager BuildManager(Action<CqrsManagerServiceConfiguration> configure)
    {
        var services = new ServiceCollection();
        services.AddKodexiaCqrs(configure);
        return services.BuildServiceProvider().GetRequiredService<ICqrsManager>();
    }

    [Fact]
    public async Task PublishAsync_InvokesAllHandlers()
    {
        var results = new List<string>();

        var services = new ServiceCollection();
        services.AddScoped(_ => results);
        services.AddKodexiaCqrs(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<NotificationPublisherTests>());

        var manager = services.BuildServiceProvider().GetRequiredService<ICqrsManager>();
        await manager.PublishAsync(new SampleNotification("test"));

        results.Should().Contain("A:test").And.Contain("B:test");
    }

    [Fact]
    public async Task PublishAsync_WithNoHandlers_DoesNotThrow()
    {
        var manager = BuildManager(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(string).Assembly)); // no handlers

        var act = async () => await manager.PublishAsync(new SampleNotification("empty"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_DeduplicatesHandlers_WhenSameTypeRegisteredTwice()
    {
        var count = 0;

        // Register two descriptors that both resolve to CountNotificationHandler (same runtime type).
        // The deduplication in NotificationHandlerWrapperImpl uses HashSet<Type>,
        // so only ONE invocation should occur.
        var services = new ServiceCollection();
        services.AddKodexiaCqrs(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(string).Assembly)); // scan empty assembly
        services.AddTransient<INotificationHandler<CountNotification>>(
            _ => new CountNotificationHandler(() => count++));
        services.AddTransient<INotificationHandler<CountNotification>>(
            _ => new CountNotificationHandler(() => count++));

        var manager = services.BuildServiceProvider().GetRequiredService<ICqrsManager>();
        await manager.PublishAsync(new CountNotification());

        count.Should().Be(1); // Two descriptors, same runtime type → deduplicated to 1 invocation
    }


    [Fact]
    public async Task PublishAsync_WithTaskWhenAllPublisher_InvokesAllConcurrently()
    {
        var bag = new System.Collections.Concurrent.ConcurrentBag<string>();

        var services = new ServiceCollection();
        services.AddScoped(_ => bag);
        services.AddKodexiaCqrs(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<NotificationPublisherTests>();
            cfg.NotificationPublisherType = typeof(TaskWhenAllPublisher);
        });

        var manager = services.BuildServiceProvider().GetRequiredService<ICqrsManager>();
        await manager.PublishAsync(new ConcurrentNotification());

        bag.Should().HaveCount(2); // Both handlers ran
    }

    [Fact]
    public async Task PublishAsync_Untyped_InvokesHandlers()
    {
        var results = new List<string>();

        var services = new ServiceCollection();
        services.AddScoped(_ => results);
        services.AddKodexiaCqrs(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<NotificationPublisherTests>());

        var manager = services.BuildServiceProvider().GetRequiredService<ICqrsManager>();
        await manager.PublishAsync((object)new SampleNotification("untyped"));

        results.Should().HaveCountGreaterThan(0);
    }
}

// ─── Test Fixtures ────────────────────────────────────────────────────────────

public record SampleNotification(string Value) : INotification;

public class SampleHandlerA(List<string> results) : INotificationHandler<SampleNotification>
{
    public Task HandleAsync(SampleNotification n, CancellationToken ct)
    {
        results.Add($"A:{n.Value}");
        return Task.CompletedTask;
    }
}

public class SampleHandlerB(List<string> results) : INotificationHandler<SampleNotification>
{
    public Task HandleAsync(SampleNotification n, CancellationToken ct)
    {
        results.Add($"B:{n.Value}");
        return Task.CompletedTask;
    }
}

public record CountNotification : INotification;
public class CountNotificationHandler(Action increment) : INotificationHandler<CountNotification>
{
    public Task HandleAsync(CountNotification n, CancellationToken ct)
    {
        increment();
        return Task.CompletedTask;
    }
}

public record ConcurrentNotification : INotification;

public class ConcurrentHandlerA(System.Collections.Concurrent.ConcurrentBag<string> bag)
    : INotificationHandler<ConcurrentNotification>
{
    public Task HandleAsync(ConcurrentNotification n, CancellationToken ct)
    {
        bag.Add("A");
        return Task.CompletedTask;
    }
}

public class ConcurrentHandlerB(System.Collections.Concurrent.ConcurrentBag<string> bag)
    : INotificationHandler<ConcurrentNotification>
{
    public Task HandleAsync(ConcurrentNotification n, CancellationToken ct)
    {
        bag.Add("B");
        return Task.CompletedTask;
    }
}
