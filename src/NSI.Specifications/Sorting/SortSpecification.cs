using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NSI.Specifications.Sorting;

/// <summary>
/// Immutable collection of ordered sort clauses.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public sealed class SortSpecification<T>: ISortSpecification<T> {
  /// <inheritdoc />
  public IReadOnlyList<ISortClause<T>> Clauses { get; }

  private SortSpecification(IReadOnlyList<ISortClause<T>> clauses) => Clauses = clauses;

  internal static SortSpecification<T> FromSingle<TKey>(Expression<Func<T, TKey>> keySelector, SortDirection direction) {
    ArgumentNullException.ThrowIfNull(keySelector);
    ISortClause<T>[] clauses = [new SortClause<T, TKey>(0, direction, keySelector)];
    return new SortSpecification<T>(clauses);
  }

  /// <summary>
  /// Adds an additional clause returning a new specification instance (immutability preserved).
  /// </summary>
  /// <typeparam name="TKey">Key type.</typeparam>
  /// <param name="keySelector">Key selector.</param>
  /// <param name="direction">Sort direction (default Ascendant).</param>
  /// <returns>New specification containing previous clauses plus the new one.</returns>
  public SortSpecification<T> Then<TKey>(Expression<Func<T, TKey>> keySelector, SortDirection direction = SortDirection.Asc) {
    ArgumentNullException.ThrowIfNull(keySelector);
    var nextIndex = Clauses.Count;
    var list = new List<ISortClause<T>>(Clauses.Count + 1);
    list.AddRange(Clauses);
    list.Add(new SortClause<T, TKey>(nextIndex, direction, keySelector));
    return new SortSpecification<T>(list);
  }
}

