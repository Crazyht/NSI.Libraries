using System.Linq.Expressions;
using NSI.Specifications.Core;

namespace NSI.Specifications.Filtering.Comparison;

/// <summary>
/// Specification filtering entities whose selected comparable value is greater than or equal to a
/// configured boundary value (inclusive lower bound comparison).
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <typeparam name="TKey">Comparable value type.</typeparam>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Evaluates <c>selector(entity) &gt;= value</c>.</description></item>
///   <item><description>Supports deep member access (null-safe guarded when path is multi-level).</description></item>
///   <item><description>Captures the comparison value as a constant node (provider translatable).</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Use for lower-bound range filters; compose with a &lt;= spec for closed ranges.</description></item>
///   <item><description>For exclusive lower bounds prefer a dedicated GreaterThan specification.</description></item>
///   <item><description>Ensure <typeparamref name="TKey"/> implements efficient <see cref="IComparable{TKey}"/> logic.</description></item>
///   <item><description>Keep selector free of side effects and avoid capturing mutable state.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>O(1) expression construction (one comparison + optional null guard chain).</description></item>
///   <item><description>No delegate compilation; deferred to consumer.</description></item>
///   <item><description>Immutable once created; safe to reuse across queries.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Immutable after construction; safe for concurrent usage.</para>
/// </remarks>
/// <example>
/// <code>
/// // Filter products priced at or above a threshold
/// var minPrice = new GreaterThanOrEqualSpecification&lt;Product, decimal&gt;(p => p.Price, 25m);
/// var query = products.AsQueryable().Where(minPrice.ToExpression());
/// </code>
/// </example>
public sealed class GreaterThanOrEqualSpecification<T, TKey>(
  Expression<Func<T, TKey>> selector,
  TKey value)
  : Specification<T>, IFilterSpecification<T> where TKey : IComparable<TKey> {
  private readonly Expression<Func<T, TKey>> _Selector = selector ?? throw new ArgumentNullException(nameof(selector));
  private readonly TKey _Value = value;

  /// <summary>
  /// Builds the predicate enforcing the inclusive lower-bound comparison (with null guards for multi-level member paths).
  /// </summary>
  /// <returns>Expression yielding <see langword="true"/> when the selected value is &gt;= configured boundary.</returns>
  public override Expression<Func<T, bool>> ToExpression() {
    var parameter = _Selector.Parameters[0];
    var body = _Selector.Body;
    var constant = Expression.Constant(_Value, typeof(TKey));
    Expression comparison = Expression.GreaterThanOrEqual(body, constant);
    if (body is MemberExpression me && me.Expression is not ParameterExpression) {
      comparison = GuardBuilder.Build(_Selector.Body, comparison, parameter);
    }
    return Expression.Lambda<Func<T, bool>>(comparison, parameter);
  }
}
