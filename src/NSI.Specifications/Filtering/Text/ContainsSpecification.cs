using System.Linq.Expressions;
using System.Reflection;
using NSI.Specifications.Core;

namespace NSI.Specifications.Filtering.Text;

/// <summary>
/// Specification filtering entities whose selected string contains a configured search term with
/// optional case-insensitive comparison (culture invariant lowering strategy for provider translation).
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Evaluates <c>selector(entity)</c> string containment against a fixed term.</description></item>
///   <item><description>Empty or null term yields a predicate that is always <c>false</c>.</description></item>
///   <item><description>Optional case-insensitive mode implemented via ordinal lower‑casing on both sides.</description></item>
///   <item><description>Null candidate strings never match (explicit null guard included).</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Pre-normalize (trim / canonicalize) input term externally when appropriate.</description></item>
///   <item><description>Prefer case-sensitive mode for performance when domain semantics allow.</description></item>
///   <item><description>Compose with other specifications using <see cref="SpecificationExtensions"/> helpers.</description></item>
///   <item><description>Use explicit culture-specific normalization outside the spec if required (this implementation is ordinal).</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Expression construction is O(1); MethodInfo lookups cached statically.</description></item>
///   <item><description>Case-insensitive path adds two <c>ToLower()</c> calls; provider may translate to LOWER().</description></item>
///   <item><description>Short-circuits quickly for empty search term (returns constant-false lambda).</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Immutable after construction; safe for concurrent reuse.</para>
/// </remarks>
/// <example>
/// <code>
/// // Case-insensitive contains on Name property
/// var containsUser = new ContainsSpecification&lt;User&gt;(u => u.Name, "alice");
/// var query = users.AsQueryable().Where(containsUser.ToExpression());
///
/// // Case-sensitive variant
/// var containsExact = new ContainsSpecification&lt;User&gt;(u => u.Code, "AbC", ignoreCase: false);
/// var exactMatches = users.AsQueryable().Where(containsExact.ToExpression());
/// </code>
/// </example>
public sealed class ContainsSpecification<T>(
  Expression<Func<T, string?>> selector,
  string term,
  bool ignoreCase = true)
  : Specification<T>, IFilterSpecification<T> {
  private static readonly MethodInfo ToLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
  private static readonly MethodInfo ContainsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

  private readonly Expression<Func<T, string?>> _Selector = selector ?? throw new ArgumentNullException(nameof(selector));
  private readonly string _Term = term ?? string.Empty;
  private readonly bool _IgnoreCase = ignoreCase;

  /// <summary>
  /// Builds the predicate expression implementing string containment with optional case-insensitivity.
  /// </summary>
  /// <returns>Expression yielding <see langword="true"/> when the selected string contains the configured term.</returns>
  /// <remarks>
  /// Shape (case-insensitive): <c>e =&gt; e.Prop != null &amp;&amp; e.Prop.ToLower().Contains(term.ToLower())</c>
  /// Shape (case-sensitive): <c>e =&gt; e.Prop != null &amp;&amp; e.Prop.Contains(term)</c>
  /// </remarks>
  public override Expression<Func<T, bool>> ToExpression() {
    if (string.IsNullOrEmpty(_Term)) {
      return _ => false; // Always false – avoids provider translation overhead.
    }

    var parameter = _Selector.Parameters[0];
    var body = _Selector.Body; // e.Prop (may be nested access)

    var candidateExpr = body;
    Expression termExpr = Expression.Constant(_Term, typeof(string));

    if (_IgnoreCase) {
      // Ordinal case-insensitive via Lower() on both operands (cross-provider translatable pattern).
      candidateExpr = Expression.Call(candidateExpr, ToLowerMethod);
      termExpr = Expression.Call(termExpr, ToLowerMethod);
    }

    var containsCall = Expression.Call(candidateExpr, ContainsMethod, termExpr);
    var notNull = Expression.NotEqual(_Selector.Body, Expression.Constant(null, typeof(string)));
    Expression predicate = Expression.AndAlso(notNull, containsCall);

    // Add guard chain for multi-level navigation access (null-safe path guarding).
    if (body is MemberExpression me && me.Expression is not ParameterExpression) {
      predicate = GuardBuilder.Build(_Selector.Body, predicate, parameter);
    }

    return Expression.Lambda<Func<T, bool>>(predicate, parameter);
  }
}
