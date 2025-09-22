using System.Linq.Expressions;

namespace NSI.Specifications.Sorting;

/// <summary>
/// Internal immutable implementation of <see cref="ISortClause{T}"/> capturing a single
/// ordering component (key selector + direction + precedence index) for entity type
/// <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Entity type being ordered.</typeparam>
/// <typeparam name="TKey">Key selector return type.</typeparam>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Hold the original key selector expression for provider translation.</description></item>
///   <item><description>Expose a boxed selector (<see cref="BoxedKeySelector"/>) enabling uniform
///     handling of heterogeneous key types.</description></item>
///   <item><description>Record deterministic precedence via <see cref="OrderIndex"/>.</description></item>
/// </list>
/// </para>
/// <para>Design Notes:
/// <list type="bullet">
///   <item><description>Boxing is deferred to an explicit <c>Expression.Convert</c> so LINQ providers can
///     still inspect the underlying member access / operations.</description></item>
///   <item><description>Instance is fully immutable; safe for caching and concurrent reuse.</description></item>
/// </list>
/// </para>
/// <para>Performance: Construction performs a single expression conversion allocation. No runtime
/// compilation occurs here; consumers may compile if executing against in-memory sequences.</para>
/// <para>Thread-safety: Immutable; all exposed properties are read-only.</para>
/// </remarks>
/// <example>
/// <code>
/// // Example (internal usage building a specification)
/// var clause = new SortClause&lt;User, string&gt;(0, SortDirection.Asc, u => u.LastName);
/// </code>
/// </example>
internal sealed class SortClause<T, TKey>: ISortClause<T> {
  /// <inheritdoc />
  public int OrderIndex { get; }

  /// <inheritdoc />
  public SortDirection Direction { get; }

  /// <inheritdoc />
  public LambdaExpression KeySelector { get; }

  /// <inheritdoc />
  public Expression<Func<T, object?>> BoxedKeySelector { get; }

  /// <summary>
  /// Initializes a new immutable clause.
  /// </summary>
  /// <param name="orderIndex">Zero-based precedence index (lower first).</param>
  /// <param name="direction">Sort direction.</param>
  /// <param name="keySelector">Non-null pure key selector expression.</param>
  /// <exception cref="ArgumentNullException">When <paramref name="keySelector"/> is null.</exception>
  public SortClause(int orderIndex, SortDirection direction, Expression<Func<T, TKey>> keySelector) {
    ArgumentNullException.ThrowIfNull(keySelector);
    OrderIndex = orderIndex;
    Direction = direction;
    KeySelector = keySelector;
    // Create boxed selector (T -> object?) preserving original parameter list.
    BoxedKeySelector = Expression.Lambda<Func<T, object?>>(
      Expression.Convert(keySelector.Body, typeof(object)),
      keySelector.Parameters);
  }
}
