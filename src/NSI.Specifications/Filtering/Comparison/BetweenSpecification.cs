using System.Linq.Expressions;
using NSI.Specifications.Core;

namespace NSI.Specifications.Filtering.Comparison;

/// <summary>
/// Specification filtering entities whose selected comparable value lies between two bounds with
/// configurable inclusivity for lower and upper edges.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <typeparam name="TKey">Comparable value type.</typeparam>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Evaluates a single comparable member selected by <paramref name="selector"/>.</description></item>
///   <item><description>Lower / upper bounds provided via constructor and treated as constants.</description></item>
///   <item><description>Edge inclusion governed by <paramref name="includeLower"/> / <paramref name="includeUpper"/> flags.</description></item>
///   <item><description>Supports multi-level navigation paths (null-safe guarded externally by <see cref="GuardBuilder"/>).</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Prefer explicit inclusivity flags to express domain intent (e.g. date ranges).</description></item>
///   <item><description>For open-ended ranges compose with separate GreaterThan / LessThan specifications.</description></item>
///   <item><description>Cache the instance when reused often (immutable design).</description></item>
///   <item><description>Ensure <typeparamref name="TKey"/> implements <see cref="IComparable{TKey}"/> efficiently (avoid boxing heavy structs).</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Creates two comparison expressions and one logical AND (O(1) construction).</description></item>
///   <item><description>Bounds captured as constants; no closures or allocations per evaluation.</description></item>
///   <item><description>Null-guard composition only applied when selector path is multi-level.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Immutable after construction; safe for concurrent usage.</para>
/// </remarks>
/// <example>
/// <code>
/// // Inclusive date range (first and last day included)
/// var dateRange = new BetweenSpecification&lt;Order, DateTime&gt;(o => o.OrderDate, startDate, endDate);
///
/// // Numeric exclusive upper bound (e.g. [min, max)) commonly used in pagination windows
/// var window = new BetweenSpecification&lt;Record, int&gt;(r => r.Offset, start, end, includeLower: true, includeUpper: false);
///
/// var filtered = orders.AsQueryable().Where(dateRange.ToExpression());
/// </code>
/// </example>
public sealed class BetweenSpecification<T, TKey>(
  Expression<Func<T, TKey>> selector,
  TKey lower,
  TKey upper,
  bool includeLower = true,
  bool includeUpper = true)
  : Specification<T>, IFilterSpecification<T> where TKey : IComparable<TKey> {
  private readonly Expression<Func<T, TKey>> _Selector = selector ?? throw new ArgumentNullException(nameof(selector));
  private readonly TKey _Lower = lower;
  private readonly TKey _Upper = upper;
  private readonly bool _IncludeLower = includeLower;
  private readonly bool _IncludeUpper = includeUpper;

  /// <summary>
  /// Builds the predicate enforcing range constraints with configured edge inclusivity.
  /// </summary>
  /// <returns>Expression evaluating to <see langword="true"/> when value lies within the configured range.</returns>
  public override Expression<Func<T, bool>> ToExpression() {
    var parameter = _Selector.Parameters[0];
    var body = _Selector.Body;
    var lowerConst = Expression.Constant(_Lower, typeof(TKey));
    var upperConst = Expression.Constant(_Upper, typeof(TKey));

    Expression lowerExpr = _IncludeLower
      ? Expression.GreaterThanOrEqual(body, lowerConst)
      : Expression.GreaterThan(body, lowerConst);

    Expression upperExpr = _IncludeUpper
      ? Expression.LessThanOrEqual(body, upperConst)
      : Expression.LessThan(body, upperConst);

    Expression combined = Expression.AndAlso(lowerExpr, upperExpr);

    if (body is MemberExpression me && me.Expression is not ParameterExpression) {
      combined = GuardBuilder.Build(_Selector.Body, combined, parameter);
    }
    return Expression.Lambda<Func<T, bool>>(combined, parameter);
  }
}
