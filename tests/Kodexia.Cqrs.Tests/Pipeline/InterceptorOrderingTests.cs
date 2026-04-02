using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kodexia.Cqrs;

namespace Kodexia.Cqrs.Tests.Pipeline;

public class InterceptorOrderingTests
{
    private static IHubExchange BuildExchange(Action<HubServiceConfiguration> configure)
    {
        var services = new ServiceCollection();
        services.AddKodexiaHub(configure);
        return services.BuildServiceProvider().GetRequiredService<IHubExchange>();
    }

    [Fact]
    public async Task Interceptor_ExecutesInRegistrationOrder()
    {
        var log = new List<string>();

        var services = new ServiceCollection();
        services.AddScoped(_ => log);
        services.AddKodexiaHub(cfg =>
        {
            cfg.RegisterHubClassesFromAssemblyContaining<InterceptorOrderingTests>();
            cfg.AddOpenInterceptor(typeof(FirstInterceptor<,>));
            cfg.AddOpenInterceptor(typeof(SecondInterceptor<,>));
        });

        var exchange = services.BuildServiceProvider().GetRequiredService<IHubExchange>();
        await exchange.DeliverAsync(new OrderedMessage());

        log.Should().Equal("first-before", "second-before", "handler", "second-after", "first-after");
    }

    [Fact]
    public async Task Interceptor_WithNoInterceptors_CallsConsumerDirectly()
    {
        var exchange = BuildExchange(cfg =>
            cfg.RegisterHubClassesFromAssemblyContaining<InterceptorOrderingTests>());

        var result = await exchange.DeliverAsync(new SimpleMessage("direct"));

        result.Should().Be("direct");
    }

    [Fact]
    public async Task Interceptor_CanShortCircuit_WithoutCallingConsumer()
    {
        var services = new ServiceCollection();
        services.AddKodexiaHub(cfg =>
        {
            cfg.RegisterHubClassesFromAssemblyContaining<InterceptorOrderingTests>();
            cfg.AddInterceptor(typeof(IInterceptor<ShortCircuitMessage, string>),
                            typeof(ShortCircuitInterceptor));
        });

        var exchange = services.BuildServiceProvider().GetRequiredService<IHubExchange>();
        var result = await exchange.DeliverAsync(new ShortCircuitMessage());

        result.Should().Be("short-circuited");
    }

    [Fact]
    public async Task PreConsumer_RunsBeforeConsumer()
    {
        var log = new List<string>();

        var services = new ServiceCollection();
        services.AddScoped(_ => log);
        services.AddKodexiaHub(cfg =>
        {
            cfg.RegisterHubClassesFromAssemblyContaining<InterceptorOrderingTests>();
            cfg.AddPreConsumer(typeof(LoggingPreConsumer<>));
        });

        var exchange = services.BuildServiceProvider().GetRequiredService<IHubExchange>();
        await exchange.DeliverAsync(new SimpleMessage("test"));

        log.Should().Contain(x => x.StartsWith("pre:"));
    }

    [Fact]
    public async Task PostConsumer_RunsAfterConsumer()
    {
        var log = new List<string>();

        var services = new ServiceCollection();
        services.AddScoped(_ => log);
        services.AddKodexiaHub(cfg =>
        {
            cfg.RegisterHubClassesFromAssemblyContaining<InterceptorOrderingTests>();
            cfg.AddPostConsumer(typeof(LoggingPostConsumer<,>));
        });

        var exchange = services.BuildServiceProvider().GetRequiredService<IHubExchange>();
        await exchange.DeliverAsync(new SimpleMessage("test"));

        log.Should().Contain(x => x.StartsWith("post:"));
    }
}

// --- Test Fixtures ---

public record OrderedMessage : IMessage<None>;
public class OrderedMessageConsumer(List<string> log) : IConsumer<OrderedMessage, None>
{
    public Task<None> ConsumeAsync(OrderedMessage message, CancellationToken ct)
    {
        log.Add("handler");
        return None.Task;
    }
}

public class FirstInterceptor<TMessage, TResponse>(List<string> log)
    : IInterceptor<TMessage, TResponse>
    where TMessage : IMessage<TResponse>
{
    public async Task<TResponse> InterceptAsync(IMessageContext<TMessage, TResponse> context, CancellationToken ct)
    {
        log.Add("first-before");
        var result = await context.NextAsync(ct);
        log.Add("first-after");
        return result;
    }
}

public class SecondInterceptor<TMessage, TResponse>(List<string> log)
    : IInterceptor<TMessage, TResponse>
    where TMessage : IMessage<TResponse>
{
    public async Task<TResponse> InterceptAsync(IMessageContext<TMessage, TResponse> context, CancellationToken ct)
    {
        log.Add("second-before");
        var result = await context.NextAsync(ct);
        log.Add("second-after");
        return result;
    }
}

public record SimpleMessage(string Value) : IMessage<string>;
public class SimpleMessageConsumer : IConsumer<SimpleMessage, string>
{
    public Task<string> ConsumeAsync(SimpleMessage message, CancellationToken ct)
        => Task.FromResult(message.Value);
}

public record ShortCircuitMessage : IMessage<string>;
public class ShortCircuitMessageConsumer : IConsumer<ShortCircuitMessage, string>
{
    public Task<string> ConsumeAsync(ShortCircuitMessage message, CancellationToken ct)
        => Task.FromResult("from-handler");
}

public class ShortCircuitInterceptor : IInterceptor<ShortCircuitMessage, string>
{
    public Task<string> InterceptAsync(IMessageContext<ShortCircuitMessage, string> context, CancellationToken ct)
        => Task.FromResult("short-circuited");
}

public class LoggingPreConsumer<TMessage>(List<string> log)
    : IPreConsumer<TMessage>
    where TMessage : IHubMessage
{
    public Task ProcessAsync(TMessage message, CancellationToken ct)
    {
        log.Add($"pre:{message}");
        return Task.CompletedTask;
    }
}

public class LoggingPostConsumer<TMessage, TResponse>(List<string> log)
    : IPostConsumer<TMessage, TResponse>
    where TMessage : IMessage<TResponse>
{
    public Task ProcessAsync(TMessage message, TResponse response, CancellationToken ct)
    {
        log.Add($"post:{message}");
        return Task.CompletedTask;
    }
}
