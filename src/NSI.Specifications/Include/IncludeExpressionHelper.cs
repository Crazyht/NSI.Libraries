using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

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
  // Cached open generic EF Core Include / ThenInclude method definitions.
  private static readonly MethodInfo IncludeOpenGeneric = GetIncludeOpenGeneric();
  private static readonly MethodInfo ThenIncludeRefOpenGeneric = GetThenIncludeRefOpenGeneric();
  private static readonly MethodInfo ThenIncludeCollOpenGeneric = GetThenIncludeCollectionOpenGeneric();

  /// <summary>Resolves Include&lt;TEntity,TProperty&gt; open generic method definition.</summary>
  private static MethodInfo GetIncludeOpenGeneric() {
    static bool IsQueryableParam(MethodInfo m) {
      var p0 = m.GetParameters()[0].ParameterType;
      return p0.IsGenericType && p0.GetGenericTypeDefinition() == typeof(IQueryable<>);
    }

    return typeof(EntityFrameworkQueryableExtensions)
      .GetMethods(BindingFlags.Public | BindingFlags.Static)
      .Where(m => m.Name == nameof(EntityFrameworkQueryableExtensions.Include))
      .Where(m => m.IsGenericMethodDefinition)
      .Where(m => m.GetGenericArguments().Length == 2)
      .Where(m => m.GetParameters().Length == 2)
      .First(IsQueryableParam);
  }

  /// <summary>Resolves ThenInclude for previous reference navigation open generic method.</summary>
  private static MethodInfo GetThenIncludeRefOpenGeneric() {
    static bool IsIncludable(MethodInfo m) {
      var p0 = m.GetParameters()[0].ParameterType;
      return p0.IsGenericType && p0.GetGenericTypeDefinition() == typeof(IIncludableQueryable<,>);
    }

    static bool IsReferencePrev(MethodInfo m) {
      var prevNavType = m.GetParameters()[0].ParameterType.GetGenericArguments()[1];
      var isEnumerable = prevNavType.IsGenericType &&
        prevNavType.GetGenericTypeDefinition() == typeof(IEnumerable<>);
      return !isEnumerable;
    }

    return typeof(EntityFrameworkQueryableExtensions)
      .GetMethods(BindingFlags.Public | BindingFlags.Static)
      .Where(m => m.Name == nameof(EntityFrameworkQueryableExtensions.ThenInclude))
      .Where(m => m.IsGenericMethodDefinition)
      .Where(m => m.GetGenericArguments().Length == 3)
      .Where(m => m.GetParameters().Length == 2)
      .Where(IsIncludable)
      .First(IsReferencePrev);
  }

  /// <summary>Resolves ThenInclude for previous collection navigation open generic method.</summary>
  private static MethodInfo GetThenIncludeCollectionOpenGeneric() {
    static bool IsIncludable(MethodInfo m) {
      var p0 = m.GetParameters()[0].ParameterType;
      return p0.IsGenericType && p0.GetGenericTypeDefinition() == typeof(IIncludableQueryable<,>);
    }

    static bool IsCollectionPrev(MethodInfo m) {
      var prevNavType = m.GetParameters()[0].ParameterType.GetGenericArguments()[1];
      return prevNavType.IsGenericType &&
        prevNavType.GetGenericTypeDefinition() == typeof(IEnumerable<>);
    }

    return typeof(EntityFrameworkQueryableExtensions)
      .GetMethods(BindingFlags.Public | BindingFlags.Static)
      .Where(m => m.Name == nameof(EntityFrameworkQueryableExtensions.ThenInclude))
      .Where(m => m.IsGenericMethodDefinition)
      .Where(m => m.GetGenericArguments().Length == 3)
      .Where(m => m.GetParameters().Length == 2)
      .Where(IsIncludable)
      .First(IsCollectionPrev);
  }

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
