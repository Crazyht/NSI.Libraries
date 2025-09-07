using System;
using System.Linq.Expressions;

namespace NSI.Specifications.Sorting;

/// <summary>
/// Factory helpers for building sort specifications.
/// </summary>
public static class Sort {
  /// <summary>
  /// Creates a <see cref="SortSpecification{T}"/> with a single clause.
  /// </summary>
  /// <typeparam name="T">Entity type.</typeparam>
  /// <typeparam name="TKey">Key type.</typeparam>
  /// <param name="keySelector">Key selector expression.</param>
  /// <param name="direction">Sort direction (default Asc).</param>
  public static SortSpecification<T> Create<T, TKey>(Expression<Func<T, TKey>> keySelector, SortDirection direction = SortDirection.Asc)
      => SortSpecification<T>.FromSingle(keySelector, direction);
}
