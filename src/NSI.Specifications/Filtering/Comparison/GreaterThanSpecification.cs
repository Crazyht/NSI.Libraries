using System.Linq.Expressions;
using NSI.Specifications.Core;

namespace NSI.Specifications.Filtering.Comparison;

/// <summary>
/// Specification filtering entities whose selected comparable value is strictly greater than a
/// configured boundary value (exclusive lower bound comparison).
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <typeparam name="TKey">Comparable value type.</typeparam>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Evaluates <c>selector(entity) &gt; value</c> (strict inequality).</description></item>
///   <item><description>Supports deep member access (adds null-guards for multi-level paths).</description></item>
///   <item><description>Comparison value captured as constant expression (provider translatable).</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Use for open range lower bounds; combine with a less-than spec for bounded windows.</description></item>
///   <item><description>For inclusive bounds prefer <see cref="GreaterThanOrEqualSpecification{T, TKey}"/>.</description></item>
///   <item><description>Ensure <typeparamref name="TKey"/> provides efficient <see cref="IComparable{TKey}"/> implementation.</description></item>
///   <item><description>Avoid capturing mutable external state inside the selector.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>O(1) expression creation (one comparison + optional guard chain).</description></item>
///   <item><description>No delegate compilation performed; defer to consumer for in-memory evaluation.</description></item>
///   <item><description>Immutable instance safe for reuse and caching.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Immutable after construction; safe for concurrent usage.</para>
/// </remarks>
/// <example>
/// <code>
/// // Filter orders whose total strictly exceeds a threshold (exclude boundary)
/// var highValue = new GreaterThanSpecification&lt;Order, decimal&gt;(o => o.Total, 100m);
/// var query = orders.AsQueryable().Where(highValue.ToExpression());
/// </code>
/// </example>
public sealed class GreaterThanSpecification<T, TKey>(
  Expression<Func<T, TKey>> selector,
  TKey value)
  : Specification<T>, IFilterSpecification<T> where TKey : IComparable<TKey> {
  private readonly Expression<Func<T, TKey>> _Selector = selector ?? throw new ArgumentNullException(nameof(selector));
  private readonly TKey _Value = value;

  /// <summary>
  /// Builds the strict greater-than comparison predicate (adds null guards for multi-level paths).
  /// </summary>
  /// <returns>Expression yielding <see langword="true"/> when selected value is &gt; configured boundary.</returns>
  public override Expression<Func<T, bool>> ToExpression() {
    var parameter = _Selector.Parameters[0];
    var body = _Selector.Body;
    var constant = Expression.Constant(_Value, typeof(TKey));
    Expression comparison = Expression.GreaterThan(body, constant);
    if (body is MemberExpression me && me.Expression is not ParameterExpression) {
      comparison = GuardBuilder.Build(_Selector.Body, comparison, parameter);
    }
    return Expression.Lambda<Func<T, bool>>(comparison, parameter);
  }
}
