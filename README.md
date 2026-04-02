<div align="center">

# Kodexia.Cqrs

A high-performance, allocation-optimized, license-free CQRS implementation for .NET.

[![NuGet](https://img.shields.io/nuget/v/Kodexia.Cqrs?style=flat-square&logo=nuget&label=NuGet&color=004880)](https://www.nuget.org/packages/Kodexia.Cqrs)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Kodexia.Cqrs?style=flat-square&logo=nuget&color=004880)](https://www.nuget.org/packages/Kodexia.Cqrs)
[![CI](https://img.shields.io/github/actions/workflow/status/girgis-adel/Kodexia.Cqrs/ci.yml?branch=main&style=flat-square&logo=github&label=CI)](https://github.com/girgis-adel/Kodexia.Cqrs/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](LICENSE.md)
[![.NET](https://img.shields.io/badge/.NET-8%20%7C%209%20%7C%2010-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)

</div>

---

## Table of Contents

- [Why Kodexia.Cqrs?](#why-kodexiacqrs)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core Concepts](#core-concepts)
  - [Requests and Handlers](#1-requests-and-handlers)
  - [Semantic CQS Interfaces](#2-semantic-cqs-interfaces)
  - [Void Commands](#3-void-commands)
  - [Notifications (Pub/Sub)](#4-notifications-pubsub)
  - [Async Streaming](#5-async-streaming)
- [Pipeline Behaviors](#pipeline-behaviors)
  - [Open Generic Behaviors](#open-generic-behaviors)
  - [Pre- and Post-Processors](#pre--and-post-processors)
  - [Exception Handling](#exception-handling)
- [Dependency Injection](#dependency-injection)
  - [Registration Options](#registration-options)
  - [Notification Publishers](#notification-publishers)
  - [Lifetimes](#lifetimes)
- [Injecting the Right Interface](#injecting-the-right-interface)
- [MediatR Migration Guide](#mediatr-migration-guide)
- [Architecture](#architecture)
- [Contributing](#contributing)

---

## Why Kodexia.Cqrs?

Kodexia.Cqrs is a production-grade, **zero-licensing-cost** alternative to MediatR. It provides **complete API parity** while addressing known performance and allocation bottlenecks:

| Capability | Kodexia.Cqrs | MediatR |
|---|---|---|
| Requests / Responses | ✅ | ✅ |
| Void Commands | ✅ | ✅ |
| Notifications (Pub/Sub) | ✅ | ✅ |
| Async Streams (`IAsyncEnumerable`) | ✅ | ✅ |
| Pipeline behaviors | ✅ | ✅ |
| Pre / Post processors | ✅ | ✅ |
| Exception handlers + actions | ✅ | ✅ |
| **License requirement** | ❌ None | ⚠️ Commercial license key required |
| **Separate contracts package** | ❌ Not needed — all contracts included | `MediatR.Contracts` for contract-only scenarios |
| Reflection at call-time | ❌ Eliminated | ✅ Present |
| Handler wrapper allocation | Cached `ConcurrentDictionary` | Cached `ConcurrentDictionary` |
| Notification deduplication | `HashSet<Type>` — O(1) | `DistinctBy` — allocates grouping |
| Source Link / symbols | ✅ `.snupkg` | ✅ `.snupkg` |

**Design goals:**
- **No reflection at dispatch time** — `MethodInfo.Invoke` is replaced with cached strongly-typed generic dispatchers.
- **Iterative pipeline** — behaviors are composed with a closure-captured index instead of recursive delegates, reducing stack depth.
- **Single package** — no separate `Abstractions` or `Contracts` package required; install one package and start.
- **No license key** — MediatR now requires a [commercial license key](https://mediatr.io) for production use. Kodexia.Cqrs is MIT-licensed with zero restrictions.

---

## Installation

```shell
dotnet add package Kodexia.Cqrs
```

Supports: **.NET 8** (LTS) · **.NET 9** · **.NET 10**

---

## Quick Start

**1. Register in your application:**

```csharp
// Program.cs
builder.Services.AddKodexiaCqrs(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
});
```

**2. Define a query:**

```csharp
public record GetUserByIdQuery(Guid UserId) : IQuery<UserDto>;

public class GetUserByIdHandler(IUserRepository repository)
    : IRequestHandler<GetUserByIdQuery, UserDto>
{
    public Task<UserDto> HandleAsync(GetUserByIdQuery request, CancellationToken ct)
        => repository.GetByIdAsync(request.UserId, ct);
}
```

**3. Dispatch from a minimal API endpoint:**

```csharp
app.MapGet("/users/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
{
    var user = await sender.SendAsync(new GetUserByIdQuery(id), ct);
    return Results.Ok(user);
});
```

That's it. Handlers are discovered automatically from the registered assemblies.

---

## Core Concepts

### 1. Requests and Handlers

A **request** encapsulates intent and its expected response type. A **handler** executes the request's logic.

```csharp
// Request definition — record for structural equality and immutability
public record CreateOrderCommand(Guid CustomerId, decimal Total) : IRequest<Guid>;

// Handler — usually one per request type (enforced by TryAdd registration)
public class CreateOrderHandler(IOrderRepository orders) : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateOrderCommand request, CancellationToken ct)
    {
        var order = Order.Create(request.CustomerId, request.Total);
        await orders.AddAsync(order, ct);
        return order.Id;
    }
}
```

**Dispatching:**

```csharp
// Strongly typed — recommended
var orderId = await sender.SendAsync(new CreateOrderCommand(customerId, 99.99m), ct);

// Untyped — for dynamic / reflection-driven dispatch
var result = await sender.SendAsync((object)command, ct);
```

### 2. Semantic CQS Interfaces

Kodexia.Cqrs provides **semantic marker interfaces** that communicate intent without changing runtime behavior:

```csharp
// Reads state — should have no side effects
public record GetOrderSummaryQuery(Guid OrderId) : IQuery<OrderSummaryDto>;

// Mutates state with a response
public record PlaceOrderCommand(CartId CartId) : ICommand<Guid>;

// Mutates state with no response
public record CancelOrderCommand(Guid OrderId) : ICommand;
```

> [!TIP]
> Use `IQuery<T>` and `ICommand` / `ICommand<T>` at the **declaration site** to enforce CQRS discipline at the type level. Handlers implement `IRequestHandler<,>` regardless of which semantic interface is used.

### 3. Void Commands

For commands with no return value, implement `IRequestHandler<TRequest>` (non-generic). Internally the library wraps the result in `Unit`, a functional-style unit type:

```csharp
public record ArchiveOrderCommand(Guid OrderId) : ICommand;

public class ArchiveOrderHandler(IOrderRepository orders) : IRequestHandler<ArchiveOrderCommand>
{
    public Task HandleAsync(ArchiveOrderCommand request, CancellationToken ct)
        => orders.ArchiveAsync(request.OrderId, ct);
}

// Dispatch — returns Task (no response value)
await sender.SendAsync(new ArchiveOrderCommand(orderId), ct);
```

`Unit` is also available directly when a behavior must return `Task<TResponse>` for a void request:

```csharp
return Unit.Task; // == Task.FromResult(Unit.Value)
```

### 4. Notifications (Pub/Sub)

Notifications are broadcast to **all registered handlers** for a given notification type. This is suitable for domain events, audit logging, and side-effect dispatch.

```csharp
// Define the event
public record OrderShippedNotification(Guid OrderId, string TrackingCode) : INotification;

// Handler A — triggers fulfillment email
public class SendTrackingEmailHandler(IEmailService email)
    : INotificationHandler<OrderShippedNotification>
{
    public Task HandleAsync(OrderShippedNotification n, CancellationToken ct)
        => email.SendTrackingAsync(n.OrderId, n.TrackingCode, ct);
}

// Handler B — records to audit log
public class AuditShipmentHandler(IAuditLog audit)
    : INotificationHandler<OrderShippedNotification>
{
    public Task HandleAsync(OrderShippedNotification n, CancellationToken ct)
        => audit.RecordAsync("order.shipped", n.OrderId, ct);
}
```

**Publishing:**

```csharp
// After creating the shipment record:
await publisher.PublishAsync(new OrderShippedNotification(order.Id, tracking.Code), ct);
```

> [!IMPORTANT]
> The **default publisher strategy is sequential** (`ForeachAwaitPublisher`). An exception in handler A stops handler B from executing. Use `TaskWhenAllPublisher` for concurrent, isolated handler execution — see [Notification Publishers](#notification-publishers).

**Synchronous handlers** can extend `NotificationHandler<T>` to avoid `return Task.CompletedTask` boilerplate:

```csharp
public class MetricsHandler : NotificationHandler<OrderShippedNotification>
{
    protected override void Handle(OrderShippedNotification n)
        => Metrics.Increment("orders.shipped");
}
```

### 5. Async Streaming

Stream handlers return `IAsyncEnumerable<TResponse>`, suitable for server-sent events, large dataset pagination, or real-time data feeds without loading all results into memory.

```csharp
public record GetAuditLogsRequest(DateOnly From, DateOnly To) : IStreamRequest<AuditLogEntry>;

public class GetAuditLogsHandler(ILogStore store) : IStreamRequestHandler<GetAuditLogsRequest, AuditLogEntry>
{
    public async IAsyncEnumerable<AuditLogEntry> HandleAsync(
        GetAuditLogsRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var entry in store.ReadRangeAsync(request.From, request.To, ct))
        {
            yield return entry;
        }
    }
}
```

**Consuming the stream:**

```csharp
// In a minimal API endpoint with SSE or chunked response:
await foreach (var entry in sender.CreateStreamAsync(new GetAuditLogsRequest(from, to), ct))
{
    await response.WriteAsync(JsonSerializer.Serialize(entry));
    await response.WriteAsync("\n");
    await response.Body.FlushAsync(ct);
}
```

**Stream pipeline behaviors** follow an identical pattern to standard behaviors:

```csharp
public class StreamAuthorizationBehavior<TRequest, TResponse>
    : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async IAsyncEnumerable<TResponse> HandleAsync(
        TRequest request,
        StreamHandlerDelegate<TResponse> next,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await _authService.RequirePermissionAsync("stream.read", ct);

        await foreach (var item in next().WithCancellation(ct))
        {
            yield return item;
        }
    }
}
```

---

## Pipeline Behaviors

Behaviors implement cross-cutting concerns (logging, validation, caching, tracing) by wrapping the request handler. They execute in **registration order**: the first registered behavior is the outermost wrapper.

```
Request → Behavior 1 → Behavior 2 → Behavior N → Handler → Behavior N (after) → … → Behavior 1 (after)
```

```csharp
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation("→ Handling {Request}", name);

        var sw = Stopwatch.StartNew();
        var response = await next(ct);
        sw.Stop();

        logger.LogInformation("← Handled {Request} in {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
        return response;
    }
}
```

### Open Generic Behaviors

Register a behavior for **all** request types with one call:

```csharp
builder.Services.AddKodexiaCqrs(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();

    // Applied to every IRequest<T> — outermost behavior runs first
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
});
```

Or use the `OpenBehavior` value object for explicit lifetimes:

```csharp
cfg.AddOpenBehaviors(new[]
{
    new OpenBehavior(typeof(LoggingBehavior<,>),    ServiceLifetime.Singleton),
    new OpenBehavior(typeof(ValidationBehavior<,>), ServiceLifetime.Transient),
});
```

**Short-circuiting:** A behavior can return early without calling `next` — useful for caching or validation:

```csharp
public class CachingBehavior<TRequest, TResponse>(IMemoryCache cache)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheable
{
    public async Task<TResponse> HandleAsync(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var key = request.CacheKey;

        if (cache.TryGetValue(key, out TResponse? cached))
            return cached!; // Short-circuit — handler never called

        var response = await next(ct);
        cache.Set(key, response, request.Ttl);
        return response;
    }
}
```

### Pre- and Post-Processors

For simpler cross-cutting logic that doesn't need full pipeline control, use pre- and post-processors. They always run in a fixed position relative to the handler:

```
PreProcessor(s) → [Behaviors] → Handler → [Behaviors] → PostProcessor(s)
```

```csharp
// Pre-processor: runs before all behaviors and the handler
public class FluentValidationPreProcessor<TRequest>(IValidator<TRequest> validator)
    : IRequestPreProcessor<TRequest>
    where TRequest : notnull
{
    public async Task ProcessAsync(TRequest request, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(request, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);
    }
}

// Post-processor: runs after the handler and all behaviors
public class AuditPostProcessor<TRequest, TResponse>(IAuditService audit)
    : IRequestPostProcessor<TRequest, TResponse>
    where TRequest : notnull
{
    public Task ProcessAsync(TRequest request, TResponse response, CancellationToken ct)
        => audit.RecordAsync(typeof(TRequest).Name, ct);
}
```

**Registration:**

```csharp
cfg.AddOpenRequestPreProcessor(typeof(FluentValidationPreProcessor<>));
cfg.AddOpenRequestPostProcessor(typeof(AuditPostProcessor<,>));
```

### Exception Handling

Exception handlers intercept thrown exceptions and can **suppress** them by providing a fallback response. Exception **actions** observe the exception as a side effect but cannot suppress it.

```csharp
// Handler: suppress the exception and return a fallback
public class NotFoundExceptionHandler
    : IRequestExceptionHandler<GetOrderByIdQuery, OrderDto, NotFoundException>
{
    public Task HandleAsync(
        GetOrderByIdQuery request,
        NotFoundException ex,
        RequestExceptionHandlerState<OrderDto> state,
        CancellationToken ct)
    {
        state.SetHandled(OrderDto.Empty); // stops exception propagation
        return Task.CompletedTask;
    }
}

// Action: fire-and-forget side effect (cannot suppress)
public class TimeoutMetricsAction
    : IRequestExceptionAction<PlaceOrderCommand, TimeoutException>
{
    public Task Execute(PlaceOrderCommand request, TimeoutException ex, CancellationToken ct)
    {
        Metrics.Increment("orders.timeout");
        return Task.CompletedTask;
    }
}
```

Exception handlers walk the **exception type hierarchy** — a handler registered for `Exception` will catch all unhandled exceptions. More specific handlers take priority due to the ordering strategy:

```csharp
// Control processing order:
cfg.RequestExceptionActionProcessorStrategy =
    RequestExceptionActionProcessorStrategy.ApplyForUnhandledExceptions; // default
```

| Strategy | Behavior |
|---|---|
| `ApplyForUnhandledExceptions` *(default)* | Exception handlers run first; actions only fire if no handler suppressed the exception |
| `ApplyForAllExceptions` | Actions always run, then handlers |

---

## Dependency Injection

### Registration Options

```csharp
builder.Services.AddKodexiaCqrs(cfg =>
{
    // Scan one or more assemblies for handlers, notifications, etc.
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.RegisterServicesFromAssemblies(
        typeof(OrdersModule).Assembly,
        typeof(PaymentsModule).Assembly);

    // Filter types during scanning (e.g. exclude test doubles)
    cfg.TypeEvaluator = t => !t.Name.EndsWith("Stub");

    // Register open generic behaviors (in execution order)
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));

    // Register a behavior for a specific closed type only
    cfg.AddBehavior(
        typeof(IPipelineBehavior<PlaceOrderCommand, Guid>),
        typeof(IdempotencyBehavior));

    // Auto-discover pre/post processors during scan
    cfg.AutoRegisterRequestProcessors = true;

    // Register explicit pre/post processors
    cfg.AddOpenRequestPreProcessor(typeof(FluentValidationPreProcessor<>));
    cfg.AddOpenRequestPostProcessor(typeof(AuditPostProcessor<,>));
});
```

### Notification Publishers

Two built-in publishers are provided. Register a custom publisher for specialized needs (e.g. outbox pattern, retry):

```csharp
// Sequential — default; guarantees order, stops on first exception
cfg.NotificationPublisher = new ForeachAwaitPublisher();

// Concurrent — all handlers start simultaneously via Task.WhenAll
cfg.NotificationPublisherType = typeof(TaskWhenAllPublisher);

// Custom — resolved from DI, full lifetime control
cfg.NotificationPublisherType = typeof(OutboxNotificationPublisher);
```

> [!WARNING]
> `TaskWhenAllPublisher` wraps all handler failures in an `AggregateException`. A fault in one handler does **not** cancel other in-flight handlers.

**Custom publisher example** (transactional outbox pattern):

```csharp
public class OutboxNotificationPublisher(IOutboxWriter outbox)
    : INotificationPublisher
{
    public async Task PublishAsync(
        IEnumerable<NotificationHandlerExecutor> handlers,
        INotification notification,
        CancellationToken ct)
    {
        // Persist to outbox table first, then relay to handlers
        await outbox.WriteAsync(notification, ct);

        foreach (var handler in handlers)
            await handler.HandlerCallback(notification, ct);
    }
}
```

### Lifetimes

The `ICqrsManager`, `ISender`, and `IPublisher` registrations default to **Transient**. Adjust when needed:

```csharp
cfg.Lifetime = ServiceLifetime.Scoped;   // recommended for web apps with EF Core
cfg.Lifetime = ServiceLifetime.Singleton; // only if all handlers are stateless singletons
```

> [!CAUTION]
> Registering as `Singleton` while injecting `Scoped` services (e.g. `DbContext`) into handlers **will cause a captive dependency bug**. Prefer `Scoped` or `Transient`.

---

## Injecting the Right Interface

Kodexia.Cqrs exposes three injectable contracts. Prefer the **narrowest** interface that satisfies your component's need:

| Interface | Use when… |
|---|---|
| `ISender` | Component only sends requests or creates streams |
| `IPublisher` | Component only publishes notifications |
| `ICqrsManager` | Component needs both sending and publishing |

```csharp
// ✅ Correct — minimal surface, clear dependency declaration
public class OrdersController(ISender sender) { … }
public class DomainEventRelay(IPublisher publisher) { … }

// ⚠️ Acceptable — both capabilities genuinely needed
public class OrderService(ICqrsManager cqrs) { … }
```

---

## MediatR Migration Guide

Migrating from MediatR to Kodexia.Cqrs requires minimal code changes. All core types are API-compatible. This guide covers every migration step.

### 1. Update package references

```shell
# Remove MediatR
dotnet remove package MediatR

# If migrating from MediatR v10 or earlier, also remove the legacy DI package:
dotnet remove package MediatR.Extensions.Microsoft.DependencyInjection

# If using the contracts-only package, remove it too (all contracts are included):
dotnet remove package MediatR.Contracts

# Add Kodexia.Cqrs
dotnet add package Kodexia.Cqrs
```

> [!NOTE]
> MediatR v11+ consolidated `MediatR.Extensions.Microsoft.DependencyInjection` into the main package. If you're already on v11+, only `dotnet remove package MediatR` is needed. The `MediatR.Contracts` package (used for API contracts, gRPC, Blazor) is also replaced — `Kodexia.Cqrs` includes all contract interfaces (`IRequest`, `INotification`, `IStreamRequest`).

### 2. Update namespaces

| MediatR | Kodexia.Cqrs |
|---|---|
| `using MediatR;` | `using Kodexia.Cqrs;` |
| `using MediatR.Pipeline;` | *(included in `Kodexia.Cqrs`)* |

### 3. Update DI registration

```diff
- services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Startup>());
+ services.AddKodexiaCqrs(cfg => cfg.RegisterServicesFromAssemblyContaining<Startup>());
```

MediatR's registration options map directly:

```diff
  services.AddMediatR(cfg => {
-     cfg.RegisterServicesFromAssembly(typeof(Startup).Assembly);
-     cfg.AddBehavior<PingPongBehavior>();
-     cfg.AddStreamBehavior<PingPongStreamBehavior>();
-     cfg.AddRequestPreProcessor<PingPreProcessor>();
-     cfg.AddRequestPostProcessor<PingPongPostProcessor>();
-     cfg.AddOpenBehavior(typeof(GenericBehavior<,>));
  });

+ services.AddKodexiaCqrs(cfg => {
+     cfg.RegisterServicesFromAssembly(typeof(Startup).Assembly);
+     cfg.AddBehavior<PingPongBehavior>();
+     cfg.AddStreamBehavior<PingPongStreamBehavior>();
+     cfg.AddOpenRequestPreProcessor(typeof(PingPreProcessor<>));
+     cfg.AddOpenRequestPostProcessor(typeof(PingPongPostProcessor<,>));
+     cfg.AddOpenBehavior(typeof(GenericBehavior<,>));
+ });
```

> [!IMPORTANT]
> MediatR requires a license key (`cfg.LicenseKey = "..."` or `Mediator.LicenseKey = "..."`). Kodexia.Cqrs has **no license key requirement** — remove any license key configuration.

### 4. Update handler method signatures

All handler methods are renamed from `Handle` to `HandleAsync` to align with .NET async naming conventions:

**Request handlers:**
```diff
- public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken ct)
+ public async Task<Guid> HandleAsync(CreateOrderCommand request, CancellationToken ct)
```

**Notification handlers:**
```diff
- public Task Handle(OrderShippedNotification notification, CancellationToken ct)
+ public Task HandleAsync(OrderShippedNotification notification, CancellationToken ct)
```

**Pipeline behaviors:**
```diff
- public async Task<TResponse> Handle(
+ public async Task<TResponse> HandleAsync(
      TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
```

**Stream handlers:**
```diff
- public IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken ct)
+ public IAsyncEnumerable<TResponse> HandleAsync(TRequest request, CancellationToken ct)
```

**Pre-processors:**
```diff
- public Task Process(TRequest request, CancellationToken ct)
+ public Task ProcessAsync(TRequest request, CancellationToken ct)
```

**Post-processors:**
```diff
- public Task Process(TRequest request, TResponse response, CancellationToken ct)
+ public Task ProcessAsync(TRequest request, TResponse response, CancellationToken ct)
```

### 5. Update dispatch calls

```diff
  // IMediator → ISender / IPublisher / ICqrsManager
- private readonly IMediator _mediator;
+ private readonly ISender _sender;       // or IPublisher / ICqrsManager

  // Sending requests
- await mediator.Send(command, ct);
+ await sender.SendAsync(command, ct);

  // Publishing notifications
- await mediator.Publish(notification, ct);
+ await publisher.PublishAsync(notification, ct);

  // Creating streams
- mediator.CreateStream(request, ct);
+ sender.CreateStreamAsync(request, ct);
```

### 6. Quick reference — interface mapping

| MediatR | Kodexia.Cqrs | Notes |
|---|---|---|
| `IMediator` | `ICqrsManager` | Combines `ISender` + `IPublisher` |
| `ISender` | `ISender` | Same name |
| `IPublisher` | `IPublisher` | Same name |
| `IRequest<T>` | `IRequest<T>` | Same |
| `IRequest` | `IRequest` | Same (void) |
| `INotification` | `INotification` | Same |
| `IStreamRequest<T>` | `IStreamRequest<T>` | Same |
| `IRequestHandler<,>` | `IRequestHandler<,>` | Method: `Handle` → `HandleAsync` |
| `INotificationHandler<>` | `INotificationHandler<>` | Method: `Handle` → `HandleAsync` |
| `IStreamRequestHandler<,>` | `IStreamRequestHandler<,>` | Method: `Handle` → `HandleAsync` |
| `IPipelineBehavior<,>` | `IPipelineBehavior<,>` | Method: `Handle` → `HandleAsync` |
| `IStreamPipelineBehavior<,>` | `IStreamPipelineBehavior<,>` | Method: `Handle` → `HandleAsync` |
| `IRequestPreProcessor<>` | `IRequestPreProcessor<>` | Method: `Process` → `ProcessAsync` |
| `IRequestPostProcessor<,>` | `IRequestPostProcessor<,>` | Method: `Process` → `ProcessAsync` |
| `IRequestExceptionHandler<,,>` | `IRequestExceptionHandler<,,>` | Method: `Handle` → `HandleAsync` |
| `IRequestExceptionAction<,>` | `IRequestExceptionAction<,>` | Same (`Execute`) |
| `Unit` | `Unit` | Same |
| `NotificationHandler<T>` | `NotificationHandler<T>` | Same (sync base class) |

### Optional: adopt semantic interfaces

Once migrated, you can optionally adopt the semantic `ICommand` / `IQuery<T>` interfaces for richer intent expression:

```diff
- public record CreateOrderCommand(Guid CustomerId) : IRequest<Guid>;
+ public record CreateOrderCommand(Guid CustomerId) : ICommand<Guid>;

- public record GetOrderQuery(Guid Id) : IRequest<OrderDto>;
+ public record GetOrderQuery(Guid Id) : IQuery<OrderDto>;
```

---

## Architecture

```
Kodexia.Cqrs/
├── Abstractions/            ← Public contracts: IRequest, ICommand, IQuery,
│   │                           INotification, IStreamRequest, Unit
│   ├── IRequest.cs
│   ├── ICommand.cs
│   ├── IQuery.cs
│   ├── INotification.cs
│   ├── IStreamRequest.cs
│   └── Unit.cs
│
├── Handlers/                ← Handler contracts consumers implement
│   ├── IRequestHandler.cs
│   ├── INotificationHandler.cs
│   ├── IStreamRequestHandler.cs
│   └── INotificationPublisher.cs
│
├── Behaviors/               ← Pipeline contracts + built-in behavior implementations
│   ├── IPipelineBehavior.cs
│   ├── IStreamPipelineBehavior.cs
│   ├── IRequestPreProcessor.cs          + RequestPreProcessorBehavior.cs
│   ├── IRequestPostProcessor.cs         + RequestPostProcessorBehavior.cs
│   ├── IRequestExceptionHandler.cs      + RequestExceptionProcessorBehavior.cs
│   ├── IRequestExceptionAction.cs       + RequestExceptionActionProcessorBehavior.cs
│   ├── RequestExceptionHandlerState.cs
│   └── RequestExceptionActionProcessorStrategy.cs
│
├── Dispatching/             ← Public runtime interfaces + CqrsManager implementation
│   ├── ISender.cs
│   ├── IPublisher.cs
│   ├── ICqrsManager.cs
│   ├── CqrsManager.cs
│   ├── NotificationHandlerExecutor.cs
│   └── OpenBehavior.cs
│
├── Publishing/              ← Built-in notification publisher strategies
│   ├── ForeachAwaitPublisher.cs
│   └── TaskWhenAllPublisher.cs
│
├── DependencyInjection/     ← IServiceCollection extensions + configuration
│   ├── CqrsManagerServiceCollectionExtensions.cs
│   ├── CqrsManagerServiceConfiguration.cs
│   └── ServiceRegistrar.cs  (internal)
│
└── Internal/                ← Implementation details (not part of public API)
    ├── HandlersOrderer.cs
    ├── ObjectDetails.cs
    └── Wrappers/
        ├── RequestHandlerWrapper.cs
        ├── NotificationHandlerWrapper.cs
        └── StreamRequestHandlerWrapper.cs
```

**Handler wrapper caching** is the key performance mechanism. On the first dispatch for a given request type, a typed wrapper is constructed and stored in a `ConcurrentDictionary<Type, …>`. Every subsequent dispatch for that type incurs **zero warming cost** — only a dictionary lookup and virtual dispatch.

**Exception dispatch** uses the same pattern: on the first exception of a given type, a `ExceptionHandlerDispatcher<TRequest, TResponse, TException>` is created via `MakeGenericType` and cached. All subsequent dispatches use the cached, strongly-typed dispatcher with no `MethodInfo.Invoke`.

---

## Contributing

Contributions are welcome. Please open an issue first to discuss significant changes.

### Branch protection

The `main` branch is protected. All changes must go through a **pull request** — direct pushes are blocked. Every PR must pass the following automated checks before merging:

| Check | Workflow | What it verifies |
|---|---|---|
| **Build & Test** | `ci.yml` | Builds (warnings-as-errors) and runs all tests on **Ubuntu** and **Windows** |
| **PR Validation** | `pr-validation.yml` | Code formatting (`dotnet format`), build, and tests |

### How to contribute

1. **Fork** the repository
2. Create a feature branch from `main`:
   ```shell
   git checkout -b feature/my-feature
   ```
3. Make your changes
4. Ensure code formatting passes:
   ```shell
   dotnet format --verify-no-changes
   ```
5. Ensure all tests pass:
   ```shell
   dotnet test -c Release --verbosity normal
   ```
6. Commit with a clear message:
   ```
   feat: add support for keyed services
   fix: handle null response in post-processor
   docs: update migration guide for v2
   test: add exception hierarchy coverage
   ```
7. Push your branch and **open a pull request** against `main`

### PR requirements

- ✅ All CI checks must pass (build, test, format)
- ✅ At least **1 approving review** from a maintainer
- ✅ Branch must be **up to date** with `main` before merging
- ✅ Conversations must be resolved
- 🔀 Merge strategy: **Squash and merge** (keeps `main` history linear)

### Running the tests locally

```shell
dotnet restore
dotnet build -c Release -p:TreatWarningsAsErrors=true
dotnet test -c Release --verbosity normal
```

### Publishing a release

Releases are fully automated via GitHub Actions. To publish a new version:

```shell
git tag v1.2.3
git push origin v1.2.3
```

The `release.yml` workflow will build, test, pack, and push `Kodexia.Cqrs` to NuGet.org automatically.

---

<div align="center">

Made with ❤️ by [Girgis Adel](https://github.com/girgis-adel) · [MIT License](LICENSE.md)

</div>
