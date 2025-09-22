using System.Linq.Expressions;

namespace NSI.Specifications.Sorting;

/// <summary>
/// Represents a single sortable key definition for an entity type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Entity type whose instances are being ordered.</typeparam>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Describe ordering intent via a key selector expression.</description></item>
///   <item><description>Capture ascending / descending direction.</description></item>
///   <item><description>Provide an explicit application sequence through <see cref="OrderIndex"/>.</description></item>
///   <item><description>Expose a boxed form (<see cref="BoxedKeySelector"/>) guaranteeing the root
///     generic entity type appears in the interface signature for uniform handling.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Use zero-based contiguous <see cref="OrderIndex"/> values when assembling multi
///     clause sorts.</description></item>
///   <item><description>Key selector expressions must be pure and side‑effect free to support provider
///     translation (e.g. EF Core).</description></item>
///   <item><description>Avoid heavy computations in key selectors; pre-compute columns where possible.</description></item>
/// </list>
/// </para>
/// <para>Performance: Implementations should materialize and cache compiled delegates only when
/// targeting in-memory sequences; queryable providers consume the expression tree directly.
/// Avoid repeated reflection or dynamic construction in sorting pipelines.</para>
/// <para>Thread-safety: Implementations should be immutable; consuming logic may apply
/// concurrently across requests.</para>
/// </remarks>
/// <example>
/// <code>
/// var clauses = new ISortClause&lt;User&gt;[] {
///   new SortClause&lt;User, string&gt;(0, SortDirection.Asc, u => u.LastName),
///   new SortClause&lt;User, string&gt;(1, SortDirection.Asc, u => u.FirstName),
///   new SortClause&lt;User, DateTime&gt;(2, SortDirection.Desc, u => u.CreatedUtc)
/// };
/// </code>
/// </example>
public interface ISortClause<T> {
  /// <summary>Gets the zero-based order index indicating application precedence.</summary>
  /// <value>Non-negative integer; lower values applied first in ordering chain.</value>
  public int OrderIndex { get; }

  /// <summary>Gets the sort direction.</summary>
  /// <value><see cref="SortDirection.Asc"/> or <see cref="SortDirection.Desc"/>.</value>
  public SortDirection Direction { get; }

  /// <summary>Gets the original (unboxed) key selector lambda.</summary>
  /// <value>Raw <see cref="LambdaExpression"/> (may return non-object value types).</value>
  public LambdaExpression KeySelector { get; }

  /// <summary>
  /// Gets the boxed key selector ensuring a consistent <c>T -&gt; object?</c> signature when
  /// aggregating heterogeneous key types.
  /// </summary>
  /// <value>Expression returning <see cref="object"/> (nullable) for uniform handling.</value>
  public Expression<Func<T, object?>> BoxedKeySelector { get; }
}
