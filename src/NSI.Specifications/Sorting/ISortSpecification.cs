namespace NSI.Specifications.Sorting;

/// <summary>
/// Aggregates an ordered collection of sort clauses for entity type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Entity type whose instances will be ordered.</typeparam>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Group multiple <see cref="ISortClause{T}"/> instances with explicit precedence.</description></item>
///   <item><description>Provide deterministic ordering semantics for repositories / query builders.</description></item>
///   <item><description>Act as a reusable, cacheable description of ordering intent.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Ensure <see cref="ISortClause{T}.OrderIndex"/> values are zero-based and contiguous.</description></item>
///   <item><description>Favor stable keys (non-volatile columns) to maintain predictable ordering.</description></item>
///   <item><description>Keep specifications immutable to allow safe reuse across threads.</description></item>
/// </list>
/// </para>
/// <para>Performance: Sorting translation relies on underlying provider (e.g. EF Core) composing
/// ordered expressions; avoid introducing heavy computed keys—prefer pre-computed / indexed
/// columns. The specification itself should be lightweight (no runtime allocations during use).</para>
/// <para>Thread-safety: Implementations must be immutable; exposed clause list should not be
/// modified after construction.</para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class UserAlphabeticalSort: ISortSpecification&lt;User&gt; {
///   public IReadOnlyList&lt;ISortClause&lt;User&gt;&gt; Clauses { get; } = new ISortClause&lt;User&gt;[] {
///     new SortClause&lt;User, string&gt;(0, SortDirection.Asc, u => u.LastName),
///     new SortClause&lt;User, string&gt;(1, SortDirection.Asc, u => u.FirstName)
///   };
/// }
///
/// // Usage: query = query.ApplySort(userAlphabeticalSort);
/// </code>
/// </example>
public interface ISortSpecification<T> {
  /// <summary>Gets the sort clauses in application (primary -> secondary -> ...) order.</summary>
  /// <value>Immutable, non-null ordered list of clauses.</value>
  public IReadOnlyList<ISortClause<T>> Clauses { get; }
}
