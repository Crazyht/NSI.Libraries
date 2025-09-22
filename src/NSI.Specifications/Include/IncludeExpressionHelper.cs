using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using NSI.Core.Common;

namespace NSI.Specifications.Include;

/// <summary>
/// Reflection-based helper that applies <see cref="IIncludeSpecification{T}"/> chains to an
/// <see cref="IQueryable{T}"/> when provider native strongly-typed chaining cannot be composed
/// directly (dynamic depth / aggregated specifications).
/// </summary>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Resolves open generic EF Core Include / ThenInclude method definitions.</description></item>
///   <item><description>Constructs closed generic methods on demand for each navigation step.</description></item>
///   <item><description>Threads Include → ThenInclude calls reflectively for arbitrary chain length.</description></item>
///   <item><description>Falls back to string-based include paths after typed chains.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Open generic MethodInfo lookups performed once (static readonly cache).</description></item>
///   <item><description>Per chain step reflection: generic method construction + Invoke (O(n) steps).</description></item>
///   <item><description>Intended for specification aggregation scenarios; outside hot loops.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: All cached MethodInfo instances are immutable; operations are stateless.</para>
/// <para>Limitations: Relies on EF Core public API surface; breaking changes may require revision.</para>
/// </remarks>
internal static class IncludeExpressionHelper {
  // Cached open generic EF Core Include / ThenInclude method definitions using MI helper.
  private static readonly MethodInfo IncludeOpenGeneric = MI
    .Of(() => EntityFrameworkQueryableExtensions.Include<object, object>(default!, default!))
    .GetGenericMethodDefinition();

  private static readonly MethodInfo ThenIncludeRefOpenGeneric = MI
    .Of(() => EntityFrameworkQueryableExtensions.ThenInclude<object, object, object>(
      (IIncludableQueryable<object, object>)default!, default!))
    .GetGenericMethodDefinition();

  private static readonly MethodInfo ThenIncludeCollOpenGeneric = MI
    .Of(() => EntityFrameworkQueryableExtensions.ThenInclude<object, object, object>(
      (IIncludableQueryable<object, IEnumerable<object>>)default!, default!))
    .GetGenericMethodDefinition();

  /// <summary>
  /// Applies all typed include chains and string include paths from the specification onto the
  /// provided queryable.
  /// </summary>
  /// <typeparam name="T">Root entity type.</typeparam>
  /// <param name="source">Initial queryable source (non-null).</param>
  /// <param name="spec">Include specification aggregating chains and string paths (non-null).</param>
  /// <returns>Queryable with all includes applied.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="source"/> or <paramref name="spec"/> is null.</exception>
  public static IQueryable<T> Apply<T>(IQueryable<T> source, IIncludeSpecification<T> spec) where T : class {
    ArgumentNullException.ThrowIfNull(source);
    ArgumentNullException.ThrowIfNull(spec);

    var current = source;
    foreach (var steps in spec.Chains.Select(c => c.Steps)) {
      if (steps.Count == 0) {
        continue;
      }

      // Initial Include
      var firstPropType = steps[0].Body.Type;
      var includeMethod = IncludeOpenGeneric.MakeGenericMethod(typeof(T), firstPropType);
      var state = includeMethod.Invoke(null, [current, steps[0]])!;

      // ThenInclude chain
      state = ApplyThenIncludes(typeof(T), state, steps);
      current = (IQueryable<T>)state;
    }

    foreach (var path in spec.StringPaths) {
      if (string.IsNullOrWhiteSpace(path)) {
        continue; // Ignore invalid / empty paths silently (could log if policy changes)
      }
      current = EntityFrameworkQueryableExtensions.Include(current, path);
    }
    return current;
  }

  /// <summary>
  /// Applies consecutive ThenInclude reflective calls for the remaining navigation steps.
  /// </summary>
  /// <param name="rootType">Root entity type.</param>
  /// <param name="state">Intermediate includable or queryable state.</param>
  /// <param name="steps">Full ordered lambda list.</param>
  /// <returns>Resulting state after all ThenInclude calls.</returns>
  private static object ApplyThenIncludes(Type rootType, object state, IReadOnlyList<LambdaExpression> steps) {
    for (var i = 1; i < steps.Count; i++) {
      var prevType = steps[i - 1].Body.Type;
      var enumerableIface = prevType == typeof(string)
        ? null
        : Array.Find(prevType.GetInterfaces(), it => it.IsGenericType &&
            it.GetGenericTypeDefinition() == typeof(IEnumerable<>));

      var isCollection = enumerableIface is not null;
      var prevElementType = isCollection
        ? enumerableIface!.GetGenericArguments()[0]
        : prevType;
      var propType = steps[i].Body.Type;

      var thenOpen = isCollection ? ThenIncludeCollOpenGeneric : ThenIncludeRefOpenGeneric;
      var thenClosed = thenOpen.MakeGenericMethod(rootType, prevElementType, propType);
      state = thenClosed.Invoke(null, [state, steps[i]])!;
    }
    return state;
  }
}
