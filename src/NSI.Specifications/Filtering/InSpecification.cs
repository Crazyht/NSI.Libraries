using System.Linq.Expressions;
using System.Reflection;
using NSI.Core.Common;
using NSI.Specifications.Core;

namespace NSI.Specifications.Filtering;

/// <summary>
/// Specification filtering entities whose selected value is contained within a fixed in‑memory set
/// (membership test) with null-safe guard chain for multi-level member access paths.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <typeparam name="TKey">Compared value type.</typeparam>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Evaluates <c>values.Contains(selector(entity))</c> (extension method semantics).</description></item>
///   <item><description>Returns constant-false predicate when provided set is empty (no matches possible).</description></item>
///   <item><description>Adds null guards for nested member access (e.g. <c>e.A.B.Id</c>).</description></item>
///   <item><description>Uses array copy of supplied sequence ensuring deterministic snapshot semantics.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Pre-normalize / de-duplicate input values externally if required by domain semantics.</description></item>
///   <item><description>For very large sets consider a different strategy (temporary table join / hash lookup).
///     Large arrays force linear membership scan for providers lacking translation optimization.</description></item>
///   <item><description>Prefer primitive / value object identifiers for translation efficiency.</description></item>
///   <item><description>Compose with additional range / text specifications via logical combinators.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Array materialization occurs once at construction.</description></item>
///   <item><description>Static cached MethodInfo for <c>Enumerable.Contains</c> avoids repeated resolution.</description></item>
///   <item><description>Predicate returns constant when value set empty (branch elimination downstream).</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Immutable after construction; safe for concurrent reuse.</para>
/// </remarks>
/// <example>
/// <code>
/// // Filter products whose status is in an allowed subset
/// var allowed = new[] { ProductStatus.Active, ProductStatus.Pending };
/// var spec = new InSpecification&lt;Product, ProductStatus&gt;(p => p.Status, allowed);
/// var query = products.AsQueryable().Where(spec.ToExpression());
/// </code>
/// </example>
public sealed class InSpecification<T, TKey>(
  Expression<Func<T, TKey>> selector,
  IEnumerable<TKey> values): Specification<T>, IFilterSpecification<T> {
  private static readonly MethodInfo ContainsMethod = MI.Of(() => Enumerable.Contains<TKey>(default!, default!));

  private readonly Expression<Func<T, TKey>> _Selector = selector ?? throw new ArgumentNullException(nameof(selector));
  private readonly TKey[] _Values = values?.ToArray() ?? throw new ArgumentNullException(nameof(values));

  /// <summary>
  /// Builds the membership predicate expression (applies null guard chain for nested access paths).
  /// </summary>
  /// <returns>Expression evaluating to <see langword="true"/> when selected value is present in the fixed set.</returns>
  /// <remarks>
  /// Shape (simple): <c>e =&gt; valueArray.Contains(e.Prop)</c>
  /// Shape (nested): <c>e =&gt; e.A != null &amp;&amp; e.A.B != null &amp;&amp; valueArray.Contains(e.A.B.Prop)</c>
  /// Empty set: returns constant <c>false</c> expression.
  /// </remarks>
  public override Expression<Func<T, bool>> ToExpression() {
    if (_Values.Length == 0) {
      return _ => false; // No values to match.
    }

    var valueArray = Expression.Constant(_Values);
    var parameter = _Selector.Parameters[0];
    var body = _Selector.Body;

    var containsCall = Expression.Call(ContainsMethod, valueArray, body);

    if (body is MemberExpression me && me.Expression is not ParameterExpression) {
      var guarded = GuardBuilder.Build(_Selector.Body, containsCall, parameter);
      return Expression.Lambda<Func<T, bool>>(guarded, parameter);
    }
    return Expression.Lambda<Func<T, bool>>(containsCall, parameter);
  }
}
