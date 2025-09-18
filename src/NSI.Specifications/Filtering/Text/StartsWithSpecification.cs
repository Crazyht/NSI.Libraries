using System.Linq.Expressions;
using System.Reflection;
using NSI.Specifications.Core;

namespace NSI.Specifications.Filtering.Text;

/// <summary>
/// Specification filtering entities whose selected string begins with a configured term with optional
/// case-insensitive comparison (ordinal lowering strategy for provider translation compatibility).
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Evaluates prefix match on the value produced by the supplied selector.</description></item>
///   <item><description>Null candidate values never match (explicit null guard).</description></item>
///   <item><description>Empty / null term yields a predicate that always returns <c>false</c>.</description></item>
///   <item><description>Case-insensitive mode implemented via ordinal <c>ToLower()</c> on both operands.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Normalize (trim, canonicalize) the search term before constructing the specification.</description></item>
///   <item><description>Prefer case-sensitive mode when domain semantics allow (potential index leverage).</description></item>
///   <item><description>Compose with other specs (e.g. <c>EndsWith</c>, range filters) via logical combinators.</description></item>
///   <item><description>Use culture-aware transformations externally if required; this implementation is ordinal.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>O(1) construction; MethodInfo lookups cached statically.</description></item>
///   <item><description>Case-insensitive path adds two Lower() calls (provider translates to LOWER()).</description></item>
///   <item><description>Short-circuits for empty term with constant-false lambda.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Immutable after construction; safe for concurrent reuse.</para>
/// </remarks>
/// <example>
/// <code>
/// // Case-insensitive prefix match on Name
/// var startsWithUser = new StartsWithSpecification&lt;User&gt;(u => u.Name, "ali");
/// var usersQuery = users.AsQueryable().Where(startsWithUser.ToExpression());
///
/// // Case-sensitive variant on Code property
/// var startsWithCode = new StartsWithSpecification&lt;Item&gt;(i => i.Code, "PRD", ignoreCase: false);
/// var items = allItems.AsQueryable().Where(startsWithCode.ToExpression());
/// </code>
/// </example>
public sealed class StartsWithSpecification<T>(
  Expression<Func<T, string?>> selector,
  string term,
  bool ignoreCase = true)
  : Specification<T>, IFilterSpecification<T> {
  private static readonly MethodInfo ToLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
  private static readonly MethodInfo StartsWithMethod = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;

  private readonly Expression<Func<T, string?>> _Selector = selector ?? throw new ArgumentNullException(nameof(selector));
  private readonly string _Term = term ?? string.Empty;
  private readonly bool _IgnoreCase = ignoreCase;

  /// <summary>
  /// Builds the predicate expression implementing prefix comparison with optional case-insensitivity.
  /// </summary>
  /// <returns>Expression yielding <see langword="true"/> when the selected string starts with the configured term.</returns>
  /// <remarks>
  /// Shape (case-insensitive): <c>e =&gt; e.Prop != null &amp;&amp; e.Prop.ToLower().StartsWith(term.ToLower())</c>
  /// Shape (case-sensitive): <c>e =&gt; e.Prop != null &amp;&amp; e.Prop.StartsWith(term)</c>
  /// </remarks>
  public override Expression<Func<T, bool>> ToExpression() {
    if (string.IsNullOrEmpty(_Term)) {
      return _ => false; // Avoids meaningless full-scan semantics.
    }

    var parameter = _Selector.Parameters[0];
    var body = _Selector.Body; // e.Prop

    var candidateExpr = body;
    Expression termExpr = Expression.Constant(_Term, typeof(string));

    if (_IgnoreCase) {
      candidateExpr = Expression.Call(candidateExpr, ToLowerMethod);
      termExpr = Expression.Call(termExpr, ToLowerMethod);
    }

    var startsWith = Expression.Call(candidateExpr, StartsWithMethod, termExpr);
    var notNull = Expression.NotEqual(_Selector.Body, Expression.Constant(null, typeof(string)));
    Expression predicate = Expression.AndAlso(notNull, startsWith);

    if (body is MemberExpression me && me.Expression is not ParameterExpression) {
      predicate = GuardBuilder.Build(_Selector.Body, predicate, parameter);
    }
    return Expression.Lambda<Func<T, bool>>(predicate, parameter);
  }
}
