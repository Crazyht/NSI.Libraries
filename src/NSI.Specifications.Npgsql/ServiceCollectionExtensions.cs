using Microsoft.Extensions.DependencyInjection;
using NSI.Specifications.Npgsql.Optimization.Npgsql;

namespace NSI.Specifications.Npgsql;

/// <summary>
/// Dependency injection extensions registering Npgsql provider-specific specification
/// optimizations (text pattern rewrites, etc.).
/// </summary>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Registers PostgreSQL ("Pg") optimization handlers once at composition time.</description></item>
///   <item><description>Provides fluent extension for service setup (startup configuration).</description></item>
/// </list>
/// </para>
/// <para>Idempotency: Underlying registry controls duplicate handling; invoke once during
/// application startup (recommended in infrastructure configuration module).</para>
/// <para>Performance: Registration executes a small number of static initializations; negligible
/// relative to overall startup.</para>
/// <para>Thread-safety: Intended for single-threaded startup phase; registry expected to handle any
/// concurrent safety if invoked multiple times.</para>
/// </remarks>
/// <example>
/// <code>
/// // During service composition (e.g. in Program.cs or a module)
/// services.AddNpgsqlSpecifications();
/// </code>
/// </example>
public static class ServiceCollectionExtensions {
  /// <summary>
  /// Registers Npgsql provider-specific optimizations for the specification pipeline.
  /// </summary>
  /// <param name="services">Target service collection (non-null).</param>
  /// <returns>The same <paramref name="services"/> instance for fluent chaining.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="services"/> is null.</exception>
  public static IServiceCollection AddNpgsqlSpecifications(this IServiceCollection services) {
    ArgumentNullException.ThrowIfNull(services);
    NpgsqlTextOptimizations.RegisterAll();
    return services;
  }
}
