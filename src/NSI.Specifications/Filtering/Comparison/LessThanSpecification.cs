using System.Linq.Expressions;
using NSI.Specifications.Core;

namespace NSI.Specifications.Filtering.Comparison;

/// <summary>
/// Specification filtering entities whose selected comparable value is strictly less than a
/// configured boundary value (exclusive upper bound comparison).
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <typeparam name="TKey">Comparable value type.</typeparam>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Evaluates <c>selector(entity) &lt; value</c> (strict inequality).</description></item>
///   <item><description>Supports deep member paths (adds null guards when selector is multi-level).</description></item>
///   <item><description>Comparison value captured as constant; provider friendly (e.g. EF Core translation).</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Use for open upper ranges; compose with a greater-or-equal spec for half‑open windows.</description></item>
///   <item><description>For inclusive bounds prefer a corresponding LessThanOrEqual specification.</description></item>
///   <item><description>Ensure <typeparamref name="TKey"/> implements efficient <see cref="IComparable{TKey}"/>.</description></item>
///   <item><description>Keep selector pure (no side effects / no mutable captured state).</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>O(1) expression construction: one comparison + optional null guard chain.</description></item>
///   <item><description>No delegate compilation performed here (deferred to consumer as needed).</description></item>
///   <item><description>Immutable instance; safe for reuse and caching.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Immutable after construction; safe for concurrent use.</para>
/// </remarks>
/// <example>
/// <code>
/// // Filter events that start strictly before a cutoff instant (exclusive upper bound)
/// var upcoming = new LessThanSpecification&lt;CalendarEvent, DateTime&gt;(e => e.StartUtc, cutoff);
/// var query = events.AsQueryable().Where(upcoming.ToExpression());
/// </code>
/// </example>
public sealed class LessThanSpecification<T, TKey>(
  Expression<Func<T, TKey>> selector,
  TKey value)
  : Specification<T>, IFilterSpecification<T> where TKey : IComparable<TKey> {
  private readonly Expression<Func<T, TKey>> _Selector = selector ?? throw new ArgumentNullException(nameof(selector));
  private readonly TKey _Value = value;

  /// <summary>
  /// Builds the strict less-than comparison predicate (adds null guards for multi-level member paths).
  /// </summary>
  /// <returns>Expression yielding <see langword="true"/> when selected value is &lt; configured boundary.</returns>
  /// <remarks>
  /// Shape: <c>entity =&gt; selector(entity) &lt; constant</c> (wrapped with guard chain if necessary).
  /// </remarks>
  public override Expression<Func<T, bool>> ToExpression() {
    var parameter = _Selector.Parameters[0];
    var body = _Selector.Body;
    var constant = Expression.Constant(_Value, typeof(TKey));
    Expression comparison = Expression.LessThan(body, constant);
    if (body is MemberExpression me && me.Expression is not ParameterExpression) {
      comparison = GuardBuilder.Build(_Selector.Body, comparison, parameter);
    }
    return Expression.Lambda<Func<T, bool>>(comparison, parameter);
  }
}
