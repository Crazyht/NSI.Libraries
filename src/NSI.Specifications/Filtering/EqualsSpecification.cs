using System.Linq.Expressions;
using NSI.Core.Common;
using NSI.Specifications.Core;

namespace NSI.Specifications.Filtering;

/// <summary>
/// Specification filtering entities whose selected value is equal to a configured comparison value
/// (null-safe guard chain for multi-level member access paths).
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <typeparam name="TKey">Compared value type.</typeparam>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Evaluates <c>selector(entity) == value</c>.</description></item>
///   <item><description>Adds null guards for multi-level (nested) member access chains to avoid NullReferenceExceptions.</description></item>
///   <item><description>Emits a translatable expression for LINQ providers (no runtime closure dependencies).</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Use for exact value equality filters; combine with range specs for broader scenarios.</description></item>
///   <item><description>Ensure selector remains pure (no side effects / no ambient mutable state capture).</description></item>
///   <item><description>Prefer immutable specification instances and reuse where possible.</description></item>
///   <item><description>For reference equality semantics explicitly compare object references externally.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>O(1) construction (one comparison + optional guard chain conjunctions).</description></item>
///   <item><description>Null guard chain only built for nested access paths.</description></item>
///   <item><description>No delegate compilation performed; callers decide if/when to compile.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Immutable after construction; safe for concurrent reuse.</para>
/// </remarks>
/// <example>
/// <code>
/// // Filter products whose CategoryId equals a fixed id
/// var spec = new EqualsSpecification&lt;Product, Guid&gt;(p => p.CategoryId, categoryId);
/// var query = products.AsQueryable().Where(spec.ToExpression());
/// </code>
/// </example>
public sealed class EqualsSpecification<T, TKey>(
  Expression<Func<T, TKey>> selector,
  TKey value): Specification<T>, IFilterSpecification<T> {
  private readonly Expression<Func<T, TKey>> _Selector = selector ?? throw new ArgumentNullException(nameof(selector));
  private readonly TKey _Value = value;

  /// <summary>
  /// Builds the equality predicate expression (adds null guard chain for multi-level member access).
  /// </summary>
  /// <returns>Expression yielding <see langword="true"/> when the selected value equals the configured comparison value.</returns>
  /// <remarks>
  /// Shape (single-level): <c>e =&gt; e.Prop == value</c>
  /// Shape (multi-level): <c>e =&gt; e.A != null &amp;&amp; e.A.B != null &amp;&amp; e.A.B.C == value</c>
  /// </remarks>
  public override Expression<Func<T, bool>> ToExpression() {
    var parameter = _Selector.Parameters[0];
    var body = _Selector.Body;
    var constant = Expression.Constant(_Value, typeof(TKey));

    if (body is MemberExpression me && me.Expression is not ParameterExpression) {
      var equality = Expression.Equal(body, constant);
      var guarded = GuardBuilder.Build(_Selector.Body, equality, parameter);
      return Expression.Lambda<Func<T, bool>>(guarded, parameter);
    }
    return Expression.Lambda<Func<T, bool>>(Expression.Equal(body, constant), parameter);
  }
}

/// <summary>
/// Helper constructing null-guard chains for nested member access paths used by specification predicates.
/// </summary>
/// <remarks>
/// <para>
/// Ensures intermediate reference / nullable members in multi-level paths are checked for null prior
/// to evaluating the terminal predicate, preventing runtime null dereference while remaining
/// provider-translatable (guards expressed as <c>&amp;&amp;</c> conjunctions).
/// </para>
/// </remarks>
internal static class GuardBuilder {
  /// <summary>
  /// Builds a composite predicate adding null guards for each nullable / reference intermediate member.
  /// </summary>
  /// <param name="pathBody">Original member access expression (e.g. <c>e.A.B.C</c>).</param>
  /// <param name="predicate">Terminal predicate relying on the fully accessed member.</param>
  /// <param name="parameter">Root lambda parameter.</param>
  /// <returns>Predicate with prepended null guards when required.</returns>
  public static Expression Build(Expression pathBody, Expression predicate, ParameterExpression parameter) {
    var chain = MemberChainExtractor.Extract(pathBody);
    if (chain.Count > 1) {
      chain.Reverse(); // root -> leaf order
    }
    if (chain.Count == 0) {
      return predicate;
    }
    var current = (Expression)parameter;
    Expression? guard = null;
    foreach (var member in chain) {
      current = Expression.MakeMemberAccess(current, member);
      var memberType = member.GetMemberType();
      if (!memberType.IsValueType || Nullable.GetUnderlyingType(memberType) != null) {
        var notNull = Expression.NotEqual(current, Expression.Constant(null, current.Type));
        guard = guard is null ? notNull : Expression.AndAlso(guard, notNull);
      }
    }
    return guard is null ? predicate : Expression.AndAlso(guard, predicate);
  }
}
