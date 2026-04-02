using Microsoft.Extensions.DependencyInjection;

namespace Kodexia.Cqrs;

/// <summary>
/// Defines an open generic interceptor and its lifetime.
/// </summary>
public record OpenInterceptor(Type OpenInterceptorType, ServiceLifetime ServiceLifetime = ServiceLifetime.Transient);
