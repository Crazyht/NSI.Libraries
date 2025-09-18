using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation;

/// <summary>
/// Concrete ambient validation context carrying a service provider and ad‑hoc per-run data.
/// </summary>
/// <remarks>
/// <para>
/// Supplies validation rules with dependency resolution via <see cref="ServiceProvider"/> and a
/// lightweight mutable <see cref="Items"/> bag for sharing transient information during a single
/// validation operation. Designed to remain lightweight and allocation conscious.
/// </para>
/// <para>
/// Semantics:
/// <list type="bullet">
///   <item><description><see cref="ServiceProvider"/> never null (falls back to inert provider in <see cref="Empty"/>).</description></item>
///   <item><description><see cref="Items"/> is isolated per context instance (not shared).</description></item>
///   <item><description>Consumers may add / overwrite keys; last write wins.</description></item>
///   <item><description>Intended lifetime: one validation pipeline execution.</description></item>
/// </list>
/// </para>
/// <para>
/// Guidelines:
/// <list type="bullet">
///   <item><description>Avoid storing large graphs or long‑lived instances inside <see cref="Items"/>.</description></item>
///   <item><description>Use primitive identifiers instead of heavy objects when possible.</description></item>
///   <item><description>Resolve scoped services lazily; do not eagerly preload unnecessary dependencies.</description></item>
///   <item><description>Do not cache the context beyond its originating validation scope.</description></item>
/// </list>
/// </para>
/// <para>
/// Thread-safety: Not thread-safe. If parallel rule execution is introduced, create separate
/// contexts or implement external synchronization around <see cref="Items"/> mutations.
/// </para>
/// <para>
/// Performance: Dictionary starts empty and grows only as needed. Typical access patterns are
/// O(1). Prefer stable, limited key sets to minimize hashing overhead and GC pressure.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // With DI scope
/// var ctx = new ValidationContext(serviceProvider);
/// ctx.Items["UserId"] = userId;
///
/// // Empty (no service resolution)
/// var empty = ValidationContext.Empty();
/// </code>
/// </example>
public sealed class ValidationContext: IValidationContext {
  /// <inheritdoc />
  public IServiceProvider ServiceProvider { get; }

  /// <inheritdoc />
  public IDictionary<string, object?> Items { get; }

  /// <summary>
  /// Creates a context bound to the given DI service provider.
  /// </summary>
  /// <param name="serviceProvider">Application service provider (scoped / root).</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider"/> is null.</exception>
  public ValidationContext(IServiceProvider serviceProvider) {
    ArgumentNullException.ThrowIfNull(serviceProvider);
    ServiceProvider = serviceProvider;
    Items = new Dictionary<string, object?>(capacity: 4); // small anticipated key set
  }

  /// <summary>
  /// Creates an inert validation context (no service resolution capability).
  /// </summary>
  /// <returns>A context whose <see cref="ServiceProvider"/> always returns null.</returns>
  public static ValidationContext Empty() => new(EmptyServiceProvider.Instance);

  /// <summary>
  /// Inert service provider used by <see cref="Empty"/>; always returns null for any service type.
  /// </summary>
  private sealed class EmptyServiceProvider: IServiceProvider {
    public static readonly EmptyServiceProvider Instance = new();
    public object? GetService(Type serviceType) => null;
  }
}
