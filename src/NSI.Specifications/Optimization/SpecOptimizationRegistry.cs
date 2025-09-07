using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NSI.Specifications.Optimization;

/// <summary>
/// Thread-safe registry mapping provider code + specification type to optimizations.
/// </summary>
public static class SpecOptimizationRegistry {
  // Key: provider code
  private static readonly ConcurrentDictionary<string, ConcurrentDictionary<Type, ConcurrentBag<IFilterOptimization>>> Store = new(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Registers an optimization for a provider code.
  /// </summary>
  public static void Register(string providerCode, IFilterOptimization optimization) {
    ArgumentNullException.ThrowIfNull(providerCode);
    ArgumentNullException.ThrowIfNull(optimization);
    var bucket = Store.GetOrAdd(providerCode, _ => new ConcurrentDictionary<Type, ConcurrentBag<IFilterOptimization>>());
    var bag = bucket.GetOrAdd(optimization.SpecificationType, _ => []);
    bag.Add(optimization);
  }

  /// <summary>
  /// Tries to find a matching optimization for the given provider and specification type.
  /// </summary>
  public static IReadOnlyList<IFilterOptimization> Get(string providerCode, Type specificationType) {
    ArgumentNullException.ThrowIfNull(providerCode);
    ArgumentNullException.ThrowIfNull(specificationType);
    if (!Store.TryGetValue(providerCode, out var bucket)) {
      return [];
    }
    // match by exact type or by open generic definition
    if (bucket.TryGetValue(specificationType, out var exact)) {
      return [.. exact];
    }
    if (specificationType.IsGenericType && bucket.TryGetValue(specificationType.GetGenericTypeDefinition(), out var open)) {
      return [.. open];
    }
    return [];
  }
}
