namespace Kodexia.Cqrs.Tests.DependencyInjection;


public class ServiceRegistrarTests
{
    [Fact]
    public void AddKodexiaCqrs_WithNoAssemblies_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var act = () => services.AddKodexiaCqrs(_ => { }); // no RegisterServicesFrom...

        act.Should().Throw<ArgumentException>()
            .WithMessage("*No assemblies*");
    }

    [Fact]
    public void AddKodexiaCqrs_RegistersICqrsManager()
    {
        var services = new ServiceCollection();
        services.AddKodexiaCqrs(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<ServiceRegistrarTests>());

        var sp = services.BuildServiceProvider();
        sp.GetService<ICqrsManager>().Should().NotBeNull();
    }

    [Fact]
    public void AddKodexiaCqrs_RegistersISenderAndIPublisher()
    {
        var services = new ServiceCollection();
        services.AddKodexiaCqrs(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<ServiceRegistrarTests>());

        var sp = services.BuildServiceProvider();
        sp.GetService<ISender>().Should().NotBeNull();
        sp.GetService<IPublisher>().Should().NotBeNull();
    }

    [Fact]
    public void AddKodexiaCqrs_ISenderAndICqrsManager_AreSameInstance()
    {
        var services = new ServiceCollection();
        services.AddKodexiaCqrs(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<ServiceRegistrarTests>();
            cfg.Lifetime = ServiceLifetime.Scoped;
        });

        var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<ICqrsManager>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        sender.Should().BeSameAs(manager);
    }

    [Fact]
    public void AddKodexiaCqrs_DiscoversSampleHandler()
    {
        var services = new ServiceCollection();
        services.AddKodexiaCqrs(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<ServiceRegistrarTests>());

        var sp = services.BuildServiceProvider();
        var handler = sp.GetService<IRequestHandler<DiscoveryRequest, string>>();

        handler.Should().NotBeNull();
    }

    [Fact]
    public void AddKodexiaCqrs_TypeEvaluator_FiltersHandlers()
    {
        var services = new ServiceCollection();
        services.AddKodexiaCqrs(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<ServiceRegistrarTests>();
            cfg.TypeEvaluator = t => t != typeof(DiscoveryRequestHandler); // exclude this handler
        });

        var sp = services.BuildServiceProvider();
        var handler = sp.GetService<IRequestHandler<DiscoveryRequest, string>>();

        handler.Should().BeNull();
    }

    [Fact]
    public void AddKodexiaCqrs_WithSingletonLifetime_ReturnsSharedInstance()
    {
        var services = new ServiceCollection();
        services.AddKodexiaCqrs(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<ServiceRegistrarTests>();
            cfg.Lifetime = ServiceLifetime.Singleton;
        });

        var sp = services.BuildServiceProvider();
        var m1 = sp.GetRequiredService<ICqrsManager>();
        var m2 = sp.GetRequiredService<ICqrsManager>();

        m1.Should().BeSameAs(m2);
    }
}

// ─── Test Fixtures ────────────────────────────────────────────────────────────

public record DiscoveryRequest : IRequest<string>;
public class DiscoveryRequestHandler : IRequestHandler<DiscoveryRequest, string>
{
    public Task<string> HandleAsync(DiscoveryRequest request, CancellationToken ct)
        => Task.FromResult("discovered");
}
