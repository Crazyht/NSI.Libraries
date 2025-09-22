using System.Linq.Expressions;

namespace NSI.Specifications.Sorting;

/// <summary>
/// Factory helpers for building <see cref="SortSpecification{T}"/> instances.
/// </summary>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Simplify creation of a single-clause immutable sort specification.</description></item>
///   <item><description>Promote explicit, strongly typed ordering via expression trees.</description></item>
///   <item><description>Ensure argument validation (fail-fast) for provided selectors.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Chain additional clauses using <see cref="SortSpecification{T}.Then{TKey}(Expression{Func{T, TKey}}, SortDirection)"/>.</description></item>
///   <item><description>Keep key selectors pure and side-effect free for provider translation.</description></item>
///   <item><description>Prefer stable, indexed columns for performant ORDER BY operations.</description></item>
/// </list>
/// </para>
/// <para>Performance: Factory adds negligible overhead (argument null check + allocation of the
/// specification). Specifications are immutable and can be cached/reused across requests.</para>
/// <para>Thread-safety: Returned objects are immutable; safe for concurrent reuse.</para>
/// </remarks>
/// <example>
/// <code>
/// // Single clause (LastName ascending)
/// var spec = Sort.Create&lt;User, string&gt;(u => u.LastName);
///
/// // Extend (Then) to add FirstName secondary ordering
/// var extended = spec.Then(u => u.FirstName);
///
/// // Apply (example repository pattern)
/// var ordered = query.ApplySort(extended);
/// </code>
/// </example>
public static class Sort {
  /// <summary>
  /// Creates a <see cref="SortSpecification{T}"/> with a single ordering clause.
  /// </summary>
  /// <typeparam name="T">Entity type being ordered.</typeparam>
  /// <typeparam name="TKey">Key selector result type.</typeparam>
  /// <param name="keySelector">Non-null key selector expression.</param>
  /// <param name="direction">Sort direction (defaults to <see cref="SortDirection.Asc"/>).</param>
  /// <returns>Immutable single-clause sort specification.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="keySelector"/> is null.</exception>
  public static SortSpecification<T> Create<T, TKey>(
    Expression<Func<T, TKey>> keySelector,
    SortDirection direction = SortDirection.Asc) {
    ArgumentNullException.ThrowIfNull(keySelector);
    return SortSpecification<T>.FromSingle(keySelector, direction);
  }
}
