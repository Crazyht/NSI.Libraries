using System.Linq.Expressions;
using System.Reflection;

namespace NSI.Specifications.Filtering;

/// <summary>
/// Utility helper extracting an ordered member access chain from a selector expression
/// (e.g. <c>x =&gt; x.A.B.C</c> becomes [C, B, A] in leaf-to-root order) while stripping boxing /
/// convert nodes. Used by guard builders to generate null-safe navigation predicates.
/// </summary>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Traverses successive <see cref="MemberExpression"/> nodes.</description></item>
///   <item><description>Removes unary <c>Convert/ConvertChecked</c> nodes (commonly introduced by lifting
///     reference/value conversions) before evaluating the next step.</description></item>
///   <item><description>Collected order is leaf-to-root (the first member in the result list corresponds
///     to the terminal accessed member). Callers may reverse to obtain root-first order.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Provide the raw body from a lambda (e.g. <c>expr.Body</c>) without prior manual
///     modification.</description></item>
///   <item><description>Only member access chains are supported; method calls or indexers terminate the walk.</description></item>
///   <item><description>Reverse the returned list when building guard chains from root to leaf.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Linear in chain depth (usually small).</description></item>
///   <item><description>No allocations besides the resulting list.</description></item>
///   <item><description>No reflection lookups beyond reading <see cref="MemberExpression.Member"/>.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Pure function with no shared state; safe for concurrent use.</para>
/// </remarks>
/// <example>
/// <code>
/// Expression&lt;Func&lt;Order, string&gt;&gt; sel = o =&gt; o.Customer.PrimaryAddress.City.Name;
/// var chain = MemberChainExtractor.Extract(sel.Body); // [Name, City, PrimaryAddress, Customer]
/// chain.Reverse(); // Root-first order if required for guard construction
/// </code>
/// </example>
internal static class MemberChainExtractor {
  /// <summary>
  /// Extracts successive member infos from a member access expression chain (leaf to root order).
  /// </summary>
  /// <param name="body">Expression body (typically <see cref="LambdaExpression.Body"/>).</param>
  /// <returns>List of <see cref="MemberInfo"/> objects leaf-first.</returns>
  public static List<MemberInfo> Extract(Expression body) {
    var members = new List<MemberInfo>();
    var current = Strip(body);
    while (current is MemberExpression m) {
      members.Add(m.Member);
      current = m.Expression is null ? null : Strip(m.Expression);
    }
    return members;
  }

  // Strips convert / boxing layers to reveal underlying member / parameter expression.
  private static Expression Strip(Expression expr) {
    while (expr.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked) {
      expr = ((UnaryExpression)expr).Operand;
    }
    return expr;
  }
}
