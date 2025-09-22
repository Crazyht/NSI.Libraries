using System.Linq.Expressions;
using NSI.Specifications.Core;

namespace NSI.Specifications.Filtering.Comparison;

/// <summary>
/// Specification filtering entities whose selected comparable value is less than or equal to a
/// configured boundary value (inclusive upper bound comparison).
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <typeparam name="TKey">Comparable value type.</typeparam>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Evaluates <c>selector(entity) &lt;= value</c>.</description></item>
///   <item><description>Supports deep member navigation (adds null guards when multi-level).</description></item>
///   <item><description>Comparison value captured as a constant (query provider translatable).</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Compose with a greater-or-equal spec for closed ranges.</description></item>
///   <item><description>Use strictly-less-than spec for exclusive upper bounds.</description></item>
///   <item><description>Keep selector pure (no side effects, avoid mutable captures).</description></item>
///   <item><description>Ensure <typeparamref name="TKey"/> implements efficient <see cref="IComparable{TKey}"/>.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>O(1) construction: one comparison + optional guard chain.</description></item>
///   <item><description>No delegate compilation; deferred to consumer.</description></item>
///   <item><description>Immutable instance safe for reuse / caching.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Immutable after construction; safe for concurrent usage.</para>
/// </remarks>
/// <example>
/// <code>
/// // Filter tasks whose due date is on or before a deadline (inclusive)
/// var dueSoon = new LessThanOrEqualSpecification&lt;TaskItem, DateTime&gt;(t => t.DueDateUtc, deadline);
/// var upcoming = tasks.AsQueryable().Where(dueSoon.ToExpression());
/// </code>
/// </example>
public sealed class LessThanOrEqualSpecification<T, TKey>(
  Expression<Func<T, TKey>> selector,
  TKey value)
  : Specification<T>, IFilterSpecification<T> where TKey : IComparable<TKey> {
  private readonly Expression<Func<T, TKey>> _Selector = selector ?? throw new ArgumentNullException(nameof(selector));
  private readonly TKey _Value = value;

  /// <summary>
  /// Builds the predicate enforcing the inclusive upper-bound comparison (adds null guards for multi-level member paths).
  /// </summary>
  /// <returns>Expression yielding <see langword="true"/> when selected value is &lt;= configured boundary.</returns>
  /// <remarks>
  /// The expression is of the form <c>entity =&gt; (selector(entity) &lt;= value)</c>.
  /// </remarks>
  public override Expression<Func<T, bool>> ToExpression() {
    var parameter = _Selector.Parameters[0];
    var body = _Selector.Body;
    var constant = Expression.Constant(_Value, typeof(TKey));
    Expression comparison = Expression.LessThanOrEqual(body, constant);
    if (body is MemberExpression me && me.Expression is not ParameterExpression) {
      comparison = GuardBuilder.Build(_Selector.Body, comparison, parameter);
    }
    return Expression.Lambda<Func<T, bool>>(comparison, parameter);
  }
}
