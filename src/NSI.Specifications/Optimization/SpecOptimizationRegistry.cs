using System.Collections.Concurrent;

namespace NSI.Specifications.Optimization;

/// <summary>
/// Thread-safe registry mapping provider codes (e.g. Pg, SqlServer) and specification types to
/// one or more <see cref="IFilterOptimization"/> implementations.
/// </summary>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Store optimizations keyed by provider code (case-insensitive).</description></item>
///   <item><description>Support lookup by exact specification type.</description></item>
///   <item><description>Support lookup by the open generic type definition when an exact match is not
///     found (e.g. <c>MySpec&lt;User&gt;</c> matches a registration for
///     <c>MySpec&lt;&gt;</c>).</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: All operations are lock-free, relying on concurrent collections. Multiple
/// registrations of the same optimization instance are allowed and will result in duplicates;
/// callers may de-duplicate on consumption if required.</para>
/// <para>Performance: Reads are O(1) dictionary lookups plus a shallow copy of the bag into a new
/// array for immutability. Empty results reuse a cached array to avoid allocations.</para>
/// </remarks>
public static class SpecOptimizationRegistry {
  // Concurrent nested structure: ProviderCode -> SpecType -> Bag(optimizations)
  private static readonly ConcurrentDictionary<string,
    ConcurrentDictionary<Type, ConcurrentBag<IFilterOptimization>>> Store =
      new(StringComparer.OrdinalIgnoreCase);

  private static readonly IFilterOptimization[] Empty = [];

  /// <summary>
  /// Registers an optimization for a provider code.
  /// </summary>
  /// <param name="providerCode">Provider short code (non-null, non-whitespace).</param>
  /// <param name="optimization">Optimization instance (non-null).</param>
  /// <exception cref="ArgumentNullException">When a parameter is null.</exception>
  /// <exception cref="ArgumentException">When <paramref name="providerCode"/> is whitespace.</exception>
  public static void Register(string providerCode, IFilterOptimization optimization) {
    ArgumentNullException.ThrowIfNull(providerCode);
    ArgumentNullException.ThrowIfNull(optimization);
    if (string.IsNullOrWhiteSpace(providerCode)) {
      throw new ArgumentException("Provider code cannot be whitespace", nameof(providerCode));
    }

    var bucket = GetOrAddBucket(providerCode);
    var bag = bucket.GetOrAdd(optimization.SpecificationType, _ => []);
    bag.Add(optimization);
  }

  /// <summary>
  /// Gets all optimizations registered for the given provider + specification type (exact or open
  /// generic match). Returns an empty list when no match is found.
  /// </summary>
  /// <param name="providerCode">Provider code used during registration (non-null, non-empty).</param>
  /// <param name="specificationType">Concrete specification type to resolve (non-null).</param>
  /// <returns>Immutable snapshot list (may be empty).</returns>
  /// <exception cref="ArgumentNullException">When a parameter is null.</exception>
  public static IReadOnlyList<IFilterOptimization> Get(string providerCode, Type specificationType) {
    ArgumentNullException.ThrowIfNull(providerCode);
    ArgumentNullException.ThrowIfNull(specificationType);
    if (!Store.TryGetValue(providerCode, out var bucket)) {
      return Empty;
    }

    // Exact type match
    if (bucket.TryGetValue(specificationType, out var exact)) {
      return [.. exact];
    }

    // Open generic definition match (e.g. FooSpec<User> vs FooSpec<>)
    if (specificationType.IsGenericType) {
      var open = specificationType.GetGenericTypeDefinition();
      if (bucket.TryGetValue(open, out var openBag)) {
        return [.. openBag];
      }
    }
    return Empty;
  }

  private static ConcurrentDictionary<Type, ConcurrentBag<IFilterOptimization>> GetOrAddBucket(string providerCode) =>
    Store.GetOrAdd(providerCode, _ => new ConcurrentDictionary<Type, ConcurrentBag<IFilterOptimization>>());
}
