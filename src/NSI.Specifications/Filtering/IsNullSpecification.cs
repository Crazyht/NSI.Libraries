using System.Linq.Expressions;
using NSI.Specifications.Core;

namespace NSI.Specifications.Filtering;

/// <summary>
/// Specification matching when a (possibly deep) reference‑type member access chain evaluates to
/// <see langword="null"/>. Any intermediate null along the navigation path yields a match.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <typeparam name="TKey">Reference member type.</typeparam>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Accepts a selector producing a reference type (nullable or not).</description></item>
///   <item><description>Decomposes nested member access (e.g. <c>e.A.B.C</c>) into a chain.</description></item>
///   <item><description>Builds an OR chain testing each segment for null (short-circuit semantics for LINQ providers).</description></item>
///   <item><description>Matches when ANY segment in the path OR the terminal value is null.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Use for presence/consistency audits where partial navigation nulls indicate missing linkage.</description></item>
///   <item><description>For only terminal null checking (ignoring intermediate nodes) implement a specialized spec.</description></item>
///   <item><description>Compose with <c>Not()</c> (spec extensions) to express non-null navigation constraints.</description></item>
///   <item><description>Keep the selector free of side effects; only pure member access expressions are supported.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Chain extraction is linear in depth (typically very small).</description></item>
///   <item><description>Single pass builds an OR expression; no reflection invocation per evaluation.</description></item>
///   <item><description>Resulting expression is provider-translatable (simple member + null comparisons).</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Immutable after construction; safe for concurrent reuse.</para>
/// </remarks>
/// <example>
/// <code>
/// // Match orders where Customer or nested Address hierarchy has any null segment
/// var spec = new IsNullSpecification&lt;Order, Address&gt;(o => o.Customer.PrimaryAddress);
/// var inconsistent = orders.AsQueryable().Where(spec.ToExpression());
/// </code>
/// </example>
public sealed class IsNullSpecification<T, TKey>(
  Expression<Func<T, TKey>> selector): Specification<T>, IFilterSpecification<T>
  where TKey : class {
  private readonly Expression<Func<T, TKey>> _Selector =
    selector ?? throw new ArgumentNullException(nameof(selector));

  /// <summary>
  /// Builds an expression that returns <see langword="true"/> when any segment of the navigation
  /// path (or the terminal value) is null.
  /// </summary>
  /// <returns>Null detection predicate expression.</returns>
  /// <remarks>
  /// Shape (single level): <c>e =&gt; e.Prop == null</c>
  /// Shape (multi-level): <c>e =&gt; e.A == null || e.A.B == null || e.A.B.C == null</c>
  /// The OR chain allows providers to short-circuit evaluation upon first null segment.
  /// </remarks>
  public override Expression<Func<T, bool>> ToExpression() {
    var param = _Selector.Parameters[0];
    var chain = MemberChainExtractor.Extract(_Selector.Body);
    if (chain.Count > 1) {
      chain.Reverse(); // root -> leaf order
    }

    // No members (selector is parameter or unsupported) – compare directly.
    if (chain.Count == 0) {
      return Expression.Lambda<Func<T, bool>>(
        Expression.Equal(_Selector.Body, Expression.Constant(null, typeof(TKey))),
        param);
    }

    Expression current = param;
    Expression? orChain = null;
    foreach (var member in chain) {
      current = Expression.MakeMemberAccess(current, member);
      var isNull = Expression.Equal(current, Expression.Constant(null, current.Type));
      orChain = orChain is null ? isNull : Expression.OrElse(orChain, isNull);
    }
    return Expression.Lambda<Func<T, bool>>(orChain!, param);
  }
}
