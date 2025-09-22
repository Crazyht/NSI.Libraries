using System.Linq.Expressions;
using System.Reflection;
using NSI.Core.Common;

namespace NSI.Specifications.Core;

/// <summary>
/// Builds null-safe predicate <see cref="Expression"/> trees from a member access path and a terminal
/// predicate, guarding each nullable reference in the chain (safe navigation for LINQ providers).
/// </summary>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Expands a path expression (e.g. <c>x =&gt; x.Address.Country.Name</c>).</description></item>
///   <item><description>Adds sequential null checks for each nullable reference/nullable struct link.</description></item>
///   <item><description>Applies the supplied leaf predicate only when all guards succeed.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Use for dynamic filtering where intermediate navigation properties may be null.</description></item>
///   <item><description>Ensure the leaf predicate is side‑effect free and provider translatable.</description></item>
///   <item><description>Avoid overly deep chains for performance; consider pre-normalizing aggregates.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Allocation proportional to member depth (O(n) expression nodes + guard list).</description></item>
///   <item><description>Single pass over chain; no reflection except <see cref="NSI.Core.Common.MemberInfoExtensions.GetMemberType(System.Reflection.MemberInfo)"/>.</description></item>
///   <item><description>Resulting expression can be cached by callers if reused.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Stateless; all methods are safe for concurrent invocation.</para>
/// </remarks>
public static class NullGuard {
  /// <summary>
  /// Creates a null-safe predicate by injecting required null checks along the provided member path.
  /// </summary>
  /// <typeparam name="T">Root entity type.</typeparam>
  /// <typeparam name="TMember">Leaf member type.</typeparam>
  /// <param name="path">Navigation path (e.g. <c>x =&gt; x.Address.Country.Name</c>).</param>
  /// <param name="predicate">Predicate applied to the leaf value when all guards pass.</param>
  /// <returns>Composed expression returning <see langword="true"/> when non-null chain satisfies the predicate.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="path"/> or <paramref name="predicate"/> is null.</exception>
  /// <example>
  /// <code>
  /// // Filters users whose company country ISO code equals "FR" guarding null intermediates
  /// Expression&lt;Func&lt;User, string?&gt;&gt; path = u =&gt; u.Company.Address.Country.Code;
  /// Expression&lt;Func&lt;string?, bool&gt;&gt; leaf = code =&gt; code == "FR";
  /// var safe = NullGuard.Safe(path, leaf); // u =&gt; u.Company != null &amp;&amp; u.Company.Address != null ... &amp;&amp; code == "FR"
  /// var filtered = users.AsQueryable().Where(safe);
  /// </code>
  /// </example>
  public static Expression<Func<T, bool>> Safe<T, TMember>(Expression<Func<T, TMember>> path, Expression<Func<TMember, bool>> predicate) {
    ArgumentNullException.ThrowIfNull(path);
    ArgumentNullException.ThrowIfNull(predicate);

    var parameter = path.Parameters[0];
    var memberChain = ExtractChain(path.Body);
    if (memberChain.Count == 0) {
      var direct = ParameterReplacer.Replace(predicate.Body, predicate.Parameters[0], parameter);
      return Expression.Lambda<Func<T, bool>>(direct, parameter);
    }

    var current = (Expression)parameter;
    var nullChecks = new List<Expression>();
    foreach (var member in memberChain) {
      current = Expression.MakeMemberAccess(current, member);
      var memberType = member.GetMemberType();
      if (!memberType.IsValueType || Nullable.GetUnderlyingType(memberType) != null) {
        nullChecks.Add(Expression.NotEqual(current, Expression.Constant(null, current.Type)));
      }
    }

    var leafAccess = current;
    var predicateBody = new LeafRebinder(predicate.Parameters[0], leafAccess).Visit(predicate.Body)!;
    var body = predicateBody;
    for (var i = nullChecks.Count - 1; i >= 0; i--) {
      body = Expression.AndAlso(nullChecks[i], body);
    }
    return Expression.Lambda<Func<T, bool>>(body, parameter);
  }

  /// <summary>
  /// Extracts the ordered member chain from an access expression (root excluded).
  /// </summary>
  private static List<System.Reflection.MemberInfo> ExtractChain(Expression body) {
    var result = new List<System.Reflection.MemberInfo>();
    var current = Strip(body);
    while (current is MemberExpression m) {
      result.Insert(0, m.Member);
      current = m.Expression is null ? null : Strip(m.Expression);
    }
    return result;
  }

  /// <summary>
  /// Removes successive convert nodes (casts) to obtain the underlying operand.
  /// </summary>
  private static Expression Strip(Expression expr) {
    while (expr.NodeType == ExpressionType.Convert || expr.NodeType == ExpressionType.ConvertChecked) {
      expr = ((UnaryExpression)expr).Operand;
    }
    return expr;
  }

  /// <summary>
  /// Rebinds the leaf predicate parameter to the constructed member access expression.
  /// </summary>
  private sealed class LeafRebinder(ParameterExpression source, Expression target): ExpressionVisitor {
    private readonly ParameterExpression _Source = source;
    private readonly Expression _Target = target;
    protected override Expression VisitParameter(ParameterExpression node) => node == _Source ? _Target : base.VisitParameter(node);
  }
}
