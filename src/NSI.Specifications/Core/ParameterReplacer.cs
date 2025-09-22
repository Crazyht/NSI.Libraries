using System.Linq.Expressions;

namespace NSI.Specifications.Core;

/// <summary>
/// Expression tree utility that replaces a specific <see cref="ParameterExpression"/> instance
/// with another inside a given expression body (parameter unification / rebinding).
/// </summary>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Traverses the expression tree once (visitor pattern).</description></item>
///   <item><description>Performs structural replacement only; does not modify other nodes.</description></item>
///   <item><description>Returns a new (possibly identical) expression instance.</description></item>
/// </list>
/// </para>
/// <para>Typical use cases:
/// <list type="bullet">
///   <item><description>Merging predicates (<c>AND/OR</c>) that originated from different parameter roots.</description></item>
///   <item><description>Adapting reusable specifications to a unified parameter in composition.</description></item>
///   <item><description>Building dynamic query pipelines requiring consistent lambda parameters.</description></item>
/// </list>
/// </para>
/// <para>Performance: Single allocation for the visitor; O(n) over node count. No caching required
/// given the low cost and ephemeral usage in composition steps.</para>
/// <para>Thread-safety: Stateless beyond constructor-captured references; safe for concurrent use
/// when invoked through the static <see cref="Replace"/> helper (creates isolated instances).</para>
/// </remarks>
/// <example>
/// <code>
/// Expression&lt;Func&lt;User, bool&gt;&gt; left  = u =&gt; u.Active;
/// Expression&lt;Func&lt;User, bool&gt;&gt; right = x =&gt; x.Age &gt;= 18;
/// // Unify 'right' parameter with 'left' parameter so they can be combined
/// var unifiedRightBody = ParameterReplacer.Replace(
///   right.Body, right.Parameters[0], left.Parameters[0]);
/// var combined = Expression.Lambda&lt;Func&lt;User, bool&gt;&gt;(
///   Expression.AndAlso(left.Body, unifiedRightBody), left.Parameters[0]);
/// </code>
/// </example>
internal sealed class ParameterReplacer: ExpressionVisitor {
  private readonly ParameterExpression _Source;
  private readonly ParameterExpression _Target;

  private ParameterReplacer(ParameterExpression source, ParameterExpression target) {
    _Source = source;
    _Target = target;
  }

  /// <summary>
  /// Replaces all occurrences of <paramref name="source"/> parameter within <paramref name="body"/>
  /// by <paramref name="target"/>.
  /// </summary>
  /// <param name="body">Expression tree root to scan.</param>
  /// <param name="source">Original parameter to replace.</param>
  /// <param name="target">Replacement parameter.</param>
  /// <returns>Transformed expression with unified parameter reference.</returns>
  internal static Expression Replace(
    Expression body,
    ParameterExpression source,
    ParameterExpression target) =>
      new ParameterReplacer(source, target).Visit(body)!;

  /// <inheritdoc />
  protected override Expression VisitParameter(ParameterExpression node)
    => node == _Source ? _Target : base.VisitParameter(node);
}
