using System.Linq.Expressions;

namespace NSI.Specifications.Core;

/// <summary>
/// Simple specification wrapping a raw predicate <see cref="Expression"/>.
/// </summary>
/// <typeparam name="T">Entity type evaluated by the specification.</typeparam>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Immutable wrapper over a provided predicate expression.</description></item>
///   <item><description>No transformation / normalization is applied to the supplied tree.</description></item>
///   <item><description>Straight passthrough for LINQ provider translation.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Use for ad‑hoc composition when a full specification subclass is unnecessary.</description></item>
///   <item><description>Prefer named specifications for reusable domain rules.</description></item>
///   <item><description>Ensure the expression is side‑effect free and provider translatable.</description></item>
/// </list>
/// </para>
/// <para>Performance: O(1) – stores reference only; no caching or compilation performed.</para>
/// <para>Thread-safety: Immutable after construction and therefore thread-safe.</para>
/// </remarks>
/// <example>
/// <code>
/// // Ad-hoc filter for active adult users
/// var spec = new ExpressionSpecification&lt;User&gt;(u => u.Active &amp;&amp; u.Age &gt;= 18);
/// var query = users.AsQueryable().Where(spec.ToExpression());
/// </code>
/// </example>
internal sealed class ExpressionSpecification<T>(Expression<Func<T, bool>> expression): Specification<T> {
  private readonly Expression<Func<T, bool>> _Expression = expression ?? throw new ArgumentNullException(nameof(expression));

  /// <inheritdoc />
  public override Expression<Func<T, bool>> ToExpression() => _Expression;
}
