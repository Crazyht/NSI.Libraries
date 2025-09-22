namespace NSI.Specifications.Sorting;

/// <summary>
/// Sort order direction applied to a key selector within a sort clause.
/// </summary>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description><see cref="Asc"/> maps typically to SQL 'ASC'.</description></item>
///   <item><description><see cref="Desc"/> maps typically to SQL 'DESC'.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Use <see cref="Asc"/> for natural ordering (alphabetical, chronological).</description></item>
///   <item><description>Pair <see cref="Desc"/> with temporal or numeric keys when recent / largest first
///     improves relevance.</description></item>
///   <item><description>Ensure deterministic ordering by adding secondary clauses when using
///     non-unique keys.</description></item>
/// </list>
/// </para>
/// <para>Stability: Whether ordering is stable depends on the underlying provider (e.g. most SQL
/// engines apply stable ordering only when complete key uniqueness is established).</para>
/// <para>Performance: Adding descending on non-indexed columns can reduce index usage; prefer
/// aligning index definitions with common query sort specifications.</para>
/// </remarks>
public enum SortDirection {
  /// <summary>Ascending order.</summary>
  Asc,
  /// <summary>Descending order.</summary>
  Desc
}
