using System.Collections.Generic;

namespace NSI.Specifications.Sorting;

/// <summary>
/// Aggregates ordered list of sort clauses.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public interface ISortSpecification<T> {
  /// <summary>Clauses in application order.</summary>
  public IReadOnlyList<ISortClause<T>> Clauses { get; }
}
