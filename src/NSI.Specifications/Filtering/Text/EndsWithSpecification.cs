using System.Linq.Expressions;
using System.Reflection;
using NSI.Specifications.Core;

namespace NSI.Specifications.Filtering.Text;

/// <summary>
/// Specification filtering entities whose selected string ends with a configured term with optional
/// case-insensitive comparison (ordinal lowering strategy for provider translation compatibility).
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Evaluates <c>selector(entity)</c> string suffix against a fixed term.</description></item>
///   <item><description>Null candidate values never match (explicit null guard).</description></item>
///   <item><description>Empty / null term returns a constant-false predicate (avoids full scan semantics).</description></item>
///   <item><description>Case-insensitive mode implemented via ordinal <c>ToLower()</c> on both operands.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Prefer case-sensitive mode when domain semantics allow for better index utilization.</description></item>
///   <item><description>Normalize incoming term (trim / canonicalize) prior to constructing the specification.</description></item>
///   <item><description>Compose with other text specifications (e.g. <c>Contains</c>, <c>StartsWith</c>) using logical combinators.</description></item>
///   <item><description>Use culture-specific comparisons externally if required; this spec is ordinal.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>O(1) construction; MethodInfo for reflection-based calls cached statically.</description></item>
///   <item><description>Case-insensitive path adds two Lower() calls (commonly translated to LOWER() in SQL).</description></item>
///   <item><description>Short-circuits for empty term returning a pre-built constant-false lambda.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Immutable after construction; safe for concurrent reuse.</para>
/// </remarks>
/// <example>
/// <code>
/// // Case-insensitive suffix match on FileName
/// var endsWithLog = new EndsWithSpecification&lt;FileRecord&gt;(f => f.FileName, ".log");
/// var logFiles = files.AsQueryable().Where(endsWithLog.ToExpression());
///
/// // Case-sensitive variant for codes
/// var endsWithCs = new EndsWithSpecification&lt;Snippet&gt;(s => s.LanguageTag, "CS", ignoreCase: false);
/// var csharp = snippets.AsQueryable().Where(endsWithCs.ToExpression());
/// </code>
/// </example>
public sealed class EndsWithSpecification<T>(
  Expression<Func<T, string?>> selector,
  string term,
  bool ignoreCase = true)
  : Specification<T>, IFilterSpecification<T> {
  private static readonly MethodInfo ToLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
  private static readonly MethodInfo EndsWithMethod = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;

  private readonly Expression<Func<T, string?>> _Selector = selector ?? throw new ArgumentNullException(nameof(selector));
  private readonly string _Term = term ?? string.Empty;
  private readonly bool _IgnoreCase = ignoreCase;

  /// <summary>
  /// Builds the predicate expression implementing suffix comparison with optional case-insensitivity.
  /// </summary>
  /// <returns>Expression yielding <see langword="true"/> when the selected string ends with the configured term.</returns>
  /// <remarks>
  /// Shape (case-insensitive): <c>e =&gt; e.Prop != null &amp;&amp; e.Prop.ToLower().EndsWith(term.ToLower())</c>
  /// Shape (case-sensitive): <c>e =&gt; e.Prop != null &amp;&amp; e.Prop.EndsWith(term)</c>
  /// </remarks>
  public override Expression<Func<T, bool>> ToExpression() {
    if (string.IsNullOrEmpty(_Term)) {
      return _ => false; // No meaningful suffix to test.
    }

    var parameter = _Selector.Parameters[0];
    var body = _Selector.Body; // e.Prop

    var candidateExpr = body;
    Expression termExpr = Expression.Constant(_Term, typeof(string));

    if (_IgnoreCase) {
      candidateExpr = Expression.Call(candidateExpr, ToLowerMethod);
      termExpr = Expression.Call(termExpr, ToLowerMethod);
    }

    var endsWith = Expression.Call(candidateExpr, EndsWithMethod, termExpr);
    var notNull = Expression.NotEqual(_Selector.Body, Expression.Constant(null, typeof(string)));
    Expression predicate = Expression.AndAlso(notNull, endsWith);

    if (body is MemberExpression me && me.Expression is not ParameterExpression) {
      predicate = GuardBuilder.Build(_Selector.Body, predicate, parameter);
    }
    return Expression.Lambda<Func<T, bool>>(predicate, parameter);
  }
}
