<div align="center">

# Kodexia Hub

A high-performance, linear-pipeline, and production-ready message exchange for .NET.

[![NuGet](https://img.shields.io/nuget/v/Kodexia.Cqrs?style=flat-square&logo=nuget&label=NuGet&color=004880)](https://www.nuget.org/packages/Kodexia.Cqrs)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Kodexia.Cqrs?style=flat-square&logo=nuget&color=004880)](https://www.nuget.org/packages/Kodexia.Cqrs)
[![CI](https://img.shields.io/github/actions/workflow/status/girgis-adel/Kodexia.Cqrs/ci.yml?branch=main&style=flat-square&logo=github&label=CI)](https://github.com/girgis-adel/Kodexia.Cqrs/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](LICENSE.md)
[![.NET](https://img.shields.io/badge/.NET-8%20%7C%209%20%7C%2010-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)

</div>

---

## What is Kodexia Hub?

Kodexia Hub is a modern, lightweight message delivery system for .NET. It facilitates the **decoupling of intent from implementation** using a distinct, high-performance architecture.

Unlike traditional "Russian Doll" recursive pipelines, Kodexia Hub uses a **linear execution chain**. This reduces stack pressure, eliminates deep recursion, and simplifies cross-cutting concerns through a clean, context-based interceptor model.

---

## Core Pillars

- **Zero Reflection at Runtime**: Dispatchers and executors are generated once and cached. No `MethodInfo.Invoke` overhead.
- **Linear Pipelines**: Interceptors are processed iteratively via an `IMessageContext`, providing better performance and easier debugging than recursive wrappers.
- **Strict Intent Abstractions**: First-class support for `IInquiry` (reads), `IAction` (writes), and `ISignal` (events).
- **Consolidated Package**: No separate abstractions or contracts package. One dependency, all features.
- **Modern .NET Native**: Tailored for .NET 8/9/10 with native DI integration and `IAsyncEnumerable` support.

---

## Installation

```shell
dotnet add package Kodexia.Cqrs
```

---

## Quick Start

### 1. Register the Hub

```csharp
// Program.cs
builder.Services.AddKodexiaHub(cfg =>
{
    cfg.RegisterHubClassesFromAssemblyContaining<Program>();
});
```

### 2. Define a Message and Consumer

```csharp
// Intent (Data)
public record GetUserInquiry(Guid Id) : IInquiry<UserDto>;

// Logic (Consumer)
public class GetUserConsumer(IDbContext db) : IConsumer<GetUserInquiry, UserDto>
{
    public Task<UserDto> ConsumeAsync(GetUserInquiry message, CancellationToken ct)
        => db.Users.FindAsync(message.Id, ct);
}
```

### 3. Deliver via the Agent

```csharp
app.MapGet("/users/{id}", async (Guid id, IDeliveryAgent agent) =>
{
    var user = await agent.DeliverAsync(new GetUserInquiry(id));
    return Results.Ok(user);
});
```

---

## The Vocabulary

Kodexia Hub uses a distinct set of abstractions to describe the flow of messages:

| Category | Type | Purpose |
|---|---|---|
| **Delivery** | `IMessage<T>` | Root interface for any delivery requiring a response. |
| | `IMessage` | A message that returns `None` (void-equivalent). |
| | `IInquiry<T>` | Semantic intent for reading data (Queries). |
| | `IAction<T>` | Semantic intent for mutating data (Commands). |
| **Broadcast** | `ISignal` | A message broadcast to multiple recipients (Events). |
| **Streaming** | `IStreamSource<T>` | A message that yields an `IAsyncEnumerable<T>`. |
| **Handlers** | `IConsumer<T, R>` | Consumes a message and returns a result. |
| | `ISubscriber<T>` | Reacts to a broadcasted signal. |
| | `IProvider<T, R>` | Provides a stream of data from a source. |
| **Agents** | `IDeliveryAgent` | Delivers messages to a single consumer. |
| | `IBroadcastAgent` | Broadcasts signals to all subscribers. |
| | `IHubExchange` | Unified interface combining delivery and broadcast. |

---

## Pipeline Interceptors

Interceptors allow you to wrap the execution of a message with cross-cutting concerns (logging, validation, caching).

```csharp
public class LoggingInterceptor<TMessage, TResponse>(ILogger logger)
    : IInterceptor<TMessage, TResponse>
    where TMessage : IMessage<TResponse>
{
    public async Task<TResponse> InterceptAsync(IMessageContext<TMessage, TResponse> context, CancellationToken ct)
    {
        logger.LogInformation("Processing {Message}", typeof(TMessage).Name);
        var result = await context.NextAsync(ct);
        logger.LogInformation("Finished {Message}", typeof(TMessage).Name);
        return result;
    }
}
```

### Processing Flow
1. **Pre-Consumers**: Simple side-effects running *before* the chain.
2. **Interceptors**: Full chain control (can short-circuit, catch exceptions, or wrap result).
3. **Consumer**: The final handler for the message.
4. **Post-Consumers**: Simple side-effects running *after* a successful result.

---

## Advanced Features

### Signal Broadcast Strategies
Control how multiple subscribers are invoked:
- `ForeachAwaitBroadcastStrategy` (Default): Sequential, stops on error.
- `TaskWhenAllBroadcastStrategy`: Concurrent, isolated execution.

### Fallback & Observation
Implement robust error handling without `try-catch` blocks in every consumer:
- `IFallbackHandler`: Provide a default result or suppress specific exceptions.
- `IExceptionObserver`: Perform side-effects (logging, telemetry) when errors occur.

---

## Performance Comparison (The Linear Chain Advantage)

| Feature | Kodexia Hub | MediatR |
|---|---|---|
| Pipeline Structure | **Linear Chain (Index-based)** | Recursive Closures (Russian Doll) |
| Stack Depth | Low (Constant) | High (Increases per behavior) |
| Reflection | None (Generated & Cached) | Present (Runtime `MakeGenericType`) |
| Contracts | Unified | Split (`MediatR.Contracts`) |
| License | **MIT (Free)** | Commercial License Required |

---

## License

Kodexia Hub is licensed under the MIT License. Use it freely in any commercial or private project without worrying about license keys or audits.
