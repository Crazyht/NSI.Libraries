using Microsoft.Extensions.DependencyInjection;
using NSI.AspNetCore.Identity;
using NSI.Core.Identity;

namespace NSI.AspNetCore;

/// <summary>
/// ASP.NET Core specific DI registration helpers for NSI identity abstractions.
/// </summary>
/// <remarks>
/// <para>
/// Consolidates common identity service registrations to ensure consistent lifetimes
/// and reduce boilerplate in host applications. Methods are additive and idempotent
/// (safe to call multiple times during startup without harmful duplication).
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description><see cref="AddNsiIdentity"/> registers HTTP-context backed user accessor only.</description></item>
///   <item><description><see cref="AddHybridUserAccessor"/> registers layered accessor (HTTP user → daemon fallback).</description></item>
///   <item><description>Both methods always ensure <c>AddHttpContextAccessor()</c> is invoked.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Use <see cref="AddNsiIdentity"/> for purely interactive web scenarios.</description></item>
///   <item><description>Use <see cref="AddHybridUserAccessor"/> when background / out-of-request flows occur.</description></item>
///   <item><description>Configure <see cref="ServiceUserSettings"/> before calling hybrid registration.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Intended for application startup single-threaded configuration. Not for
/// dynamic runtime mutation after container build.</para>
/// <para>Performance: Minimal overhead (a handful of service descriptor additions). No reflection
/// or scanning operations executed here.</para>
/// </remarks>
public static class ServiceCollectionExtensions {
  /// <summary>
  /// Registers HTTP-context based user accessor (<see cref="HttpContextUserAccessor"/>).
  /// </summary>
  /// <param name="services">Target service collection (not null).</param>
  /// <returns>The same service collection to enable fluent chaining.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="services"/> is null.</exception>
  /// <remarks>
  /// Adds:
  /// <list type="bullet">
  ///   <item><description>HTTP context accessor registration.</description></item>
  ///   <item><description><see cref="IUserAccessor"/> mapped to <see cref="HttpContextUserAccessor"/> (Scoped)</description></item>
  /// </list>
  /// Prefer this for strictly interactive APIs (no background execution needing a system identity).
  /// </remarks>
  public static IServiceCollection AddNsiIdentity(this IServiceCollection services) {
    ArgumentNullException.ThrowIfNull(services);

    services.AddHttpContextAccessor();
    services.AddScoped<IUserAccessor, HttpContextUserAccessor>();
    return services;
  }

  /// <summary>
  /// Registers hybrid user accessor that falls back to daemon/system identity when HTTP user absent.
  /// </summary>
  /// <param name="services">Target service collection (not null).</param>
  /// <returns>The same service collection for fluent chaining.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="services"/> is null.</exception>
  /// <remarks>
  /// Adds (if not already present):
  /// <list type="bullet">
  ///   <item><description>HTTP context accessor.</description></item>
  ///   <item><description><see cref="HttpContextUserAccessor"/> (Scoped) for primary retrieval.</description></item>
  ///   <item><description><see cref="DaemonUserAccessor"/> (Singleton) as system identity provider.</description></item>
  ///   <item><description><see cref="IUserAccessor"/> mapped to <see cref="HybridUserAccessor"/> (Scoped).</description></item>
  /// </list>
  /// Ensure <see cref="ServiceUserSettings"/> is configured (via <c>services.Configure&lt;ServiceUserSettings&gt;(...)</c>).
  /// </remarks>
  public static IServiceCollection AddHybridUserAccessor(this IServiceCollection services) {
    ArgumentNullException.ThrowIfNull(services);

    services.AddHttpContextAccessor();
    services.AddScoped<HttpContextUserAccessor>();
    services.AddSingleton<DaemonUserAccessor>();
    services.AddScoped<IUserAccessor, HybridUserAccessor>();
    return services;
  }
}
