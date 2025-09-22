using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NSI.Core.Identity;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core;

/// <summary>
/// DI registration helpers for NSI.Core services (validation, identity helpers, utilities).
/// </summary>
/// <remarks>
/// <para>
/// Centralizes typical service wiring to ensure consistent lifetimes and reduce boilerplate in
/// host applications. All methods follow a fluent pattern returning the supplied
/// <see cref="IServiceCollection"/> to enable chaining.
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Never replaces existing registrations explicitly added by the consumer.</description></item>
///   <item><description>Validator scanning only registers concrete non-abstract, non-generic types implementing <see cref="IValidator{T}"/>.</description></item>
///   <item><description>Idempotent: multiple invocations produce no duplicate service mappings.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Prefer assembly-scoped validator registration during startup (cold path).</description></item>
///   <item><description>Use <see cref="AddValidator{TModel,TValidator}(IServiceCollection,ServiceLifetime)"/> for bespoke lifetimes.</description></item>
///   <item><description>Keep assemblies list minimal to reduce reflection cost.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Methods are safe to call during startup single-thread initialization. Not
/// intended for dynamic runtime mutation after container build.</para>
/// <para>Performance: Reflection scanning occurs only once per call; uses hashing to avoid duplicate
/// registrations. Typical usage keeps overhead negligible at application start.</para>
/// </remarks>
public static class ServiceCollectionExtensions {
  /// <summary>
  /// Registers daemon (service) user accessor and binds <see cref="ServiceUserSettings"/> from config.
  /// </summary>
  /// <param name="services">Target service collection (not null).</param>
  /// <param name="configuration">Configuration root or section containing ServiceUserSettings.</param>
  /// <returns>The same service collection for chaining.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="services"/> or <paramref name="configuration"/> is null.</exception>
  /// <remarks>
  /// <para>
  /// Binds the configuration section named "ServiceUserSettings" into options and exposes both the
  /// options wrapper and the concrete POCO for direct injection. Registers <see cref="DaemonUserAccessor"/>
  /// as <see cref="IUserAccessor"/> (singleton) for non-interactive/background contexts.
  /// </para>
  /// </remarks>
  public static IServiceCollection AddDaemonUserAccessor(
    this IServiceCollection services,
    IConfiguration configuration) {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configuration);

    services.Configure<ServiceUserSettings>(options =>
      configuration.GetSection("ServiceUserSettings").Bind(options));

    services.AddSingleton(sp => sp.GetRequiredService<IOptions<ServiceUserSettings>>().Value);
    services.AddSingleton<IUserAccessor, DaemonUserAccessor>();
    return services;
  }

  /// <summary>
  /// Scans provided assemblies (or calling assembly) and registers all <see cref="IValidator{T}"/> implementations.
  /// </summary>
  /// <param name="services">Target service collection (not null).</param>
  /// <param name="assemblies">Assemblies to scan; when empty uses calling assembly.</param>
  /// <returns>The same service collection for chaining.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="services"/> or <paramref name="assemblies"/> is null.</exception>
  /// <remarks>
  /// <para>
  /// A validator type is registered with scoped lifetime if:
  /// <list type="bullet">
  ///   <item><description>It is a non-abstract, non-generic class.</description></item>
  ///   <item><description>It implements one or more closed <see cref="IValidator{T}"/> interfaces.</description></item>
  /// </list>
  /// Duplicate (interface, implementation) pairs are ignored via TryAdd semantics.
  /// </para>
  /// </remarks>
  public static IServiceCollection AddValidators(
    this IServiceCollection services,
    params Assembly[] assemblies) {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(assemblies);

    if (assemblies.Length == 0) {
      assemblies = [Assembly.GetCallingAssembly()];
    }

    var seen = new HashSet<(Type iface, Type impl)>();
    foreach (var asm in assemblies.Where(a => a is not null)) {
      RegisterValidatorsFromAssembly(services, asm, seen);
    }
    return services;

    static void RegisterValidatorsFromAssembly(
      IServiceCollection services,
      Assembly assembly,
      HashSet<(Type iface, Type impl)> seen) {
      var validatorOpen = typeof(IValidator<>);
      foreach (var type in SafeGetTypes(assembly)) {
        if (!IsConcreteValidatorCandidate(type)) {
          continue;
        }
        RegisterValidatorInterfaces(services, type, validatorOpen, seen);
      }
    }

    static void RegisterValidatorInterfaces(
      IServiceCollection services,
      Type implementation,
      Type validatorOpen,
      HashSet<(Type iface, Type impl)> seen) {
      foreach (var iface in implementation.GetInterfaces()) {
        if (!IsValidatorInterface(iface, validatorOpen)) {
          continue;
        }
        if (!seen.Add((iface, implementation))) {
          continue;
        }
        services.TryAddScoped(iface, implementation);
      }
    }

    static IEnumerable<Type> SafeGetTypes(Assembly asm) {
      try {
        return asm.GetTypes();
      } catch (ReflectionTypeLoadException ex) {
        return ex.Types.Where(t => t is not null)!;
      }
    }

    static bool IsValidatorInterface(Type iface, Type open) =>
      iface.IsGenericType && iface.GetGenericTypeDefinition() == open;

    static bool IsConcreteValidatorCandidate(Type t) =>
      t is { IsClass: true, IsAbstract: false } && !t.IsGenericTypeDefinition;
  }

  /// <summary>
  /// Registers validators from the assembly containing the marker <typeparamref name="T"/>.
  /// </summary>
  public static IServiceCollection AddValidatorsFromAssemblyContaining<T>(
    this IServiceCollection services) =>
    services is null
      ? throw new ArgumentNullException(nameof(services))
      : services.AddValidators(typeof(T).Assembly);

  /// <summary>
  /// Registers a specific validator implementation with chosen lifetime.
  /// </summary>
  /// <typeparam name="TModel">Validated model type.</typeparam>
  /// <typeparam name="TValidator">Concrete validator implementing <see cref="IValidator{TModel}"/>.</typeparam>
  /// <param name="services">Target collection.</param>
  /// <param name="lifetime">Service lifetime (default Scoped).</param>
  /// <returns>The same service collection.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="services"/> is null.</exception>
  /// <exception cref="ArgumentOutOfRangeException">Invalid <paramref name="lifetime"/> value.</exception>
  public static IServiceCollection AddValidator<TModel, TValidator>(
    this IServiceCollection services,
    ServiceLifetime lifetime = ServiceLifetime.Scoped)
    where TValidator : class, IValidator<TModel> {
    ArgumentNullException.ThrowIfNull(services);

    return lifetime switch {
      ServiceLifetime.Singleton => services.AddSingleton<IValidator<TModel>, TValidator>(),
      ServiceLifetime.Scoped => services.AddScoped<IValidator<TModel>, TValidator>(),
      ServiceLifetime.Transient => services.AddTransient<IValidator<TModel>, TValidator>(),
      _ => throw new ArgumentOutOfRangeException(nameof(lifetime))
    };
  }
}
