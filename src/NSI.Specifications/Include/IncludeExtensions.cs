using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace NSI.Specifications.Include;

/// <summary>
/// Extension methods to apply include specifications.
/// </summary>
public static class IncludeExtensions {
  /// <summary>
  /// Applies the include specification to the queryable using EF Core Include/ThenInclude.
  /// </summary>
  public static IQueryable<T> Include<T>(this IQueryable<T> source, IIncludeSpecification<T> spec) where T : class
      => IncludeExpressionHelper.Apply(source, spec);

  /// <summary>
  /// No-op for IEnumerable sources.
  /// </summary>
  [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "IEnumerable Include is a documented no-op")]
  public static IEnumerable<T> Include<T>(this IEnumerable<T> source, IIncludeSpecification<T> spec) => source;
}
