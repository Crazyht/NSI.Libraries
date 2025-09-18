using System.Linq.Expressions;
using NSI.Specifications.Core;

namespace NSI.Specifications.Filtering;

/// <summary>
/// Specification matching entities where the selected string or collection value is empty.
/// Null values are NEVER considered empty and therefore do not satisfy the specification.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Supported target types: <see cref="string"/> and types implementing
///     <c>ICollection</c> (via <c>Count</c> property).</description></item>
///   <item><description>Null values are excluded (predicate requires non-null + length/count == 0).</description></item>
///   <item><description>Value types boxed through the selector are unwrapped (strip Convert unary node).</description></item>
///   <item><description>Nested member access paths are guarded for null-safe evaluation.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Use alongside a complementary <c>IsNotEmptySpecification</c> (future) for clarity.</description></item>
///   <item><description>Prefer domain-specific rules (e.g. Required + Length) when richer feedback needed.</description></item>
///   <item><description>Do not treat whitespace-only strings as empty — apply trimming externally if required.</description></item>
///   <item><description>Combine with other specifications via logical AND/OR for precise filters.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Expression construction is O(1); no reflection beyond property name access.</description></item>
///   <item><description>Navigation guard chain only added for multi-level member access.</description></item>
///   <item><description>No delegate compilation performed; caller may cache compiled predicate if needed.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Immutable after construction; safe for concurrent reuse.</para>
/// </remarks>
/// <example>
/// <code>
/// // Filter users with an empty Tags collection (not null, Count == 0)
/// var spec = new IsEmptySpecification&lt;User&gt;(u => u.Tags);
/// var usersWithNoTags = users.AsQueryable().Where(spec.ToExpression());
///
/// // Filter documents whose Title is the empty string
/// var titleEmpty = new IsEmptySpecification&lt;Document&gt;(d => d.Title);
/// var untitled = docs.AsQueryable().Where(titleEmpty.ToExpression());
/// </code>
/// </example>
public sealed class IsEmptySpecification<T>(
  Expression<Func<T, object?>> selector): Specification<T>, IFilterSpecification<T> {
  private readonly Expression<Func<T, object?>> _Selector =
    selector ?? throw new ArgumentNullException(nameof(selector));

  /// <summary>
  /// Builds the emptiness predicate expression (non-null + length/count == 0) with navigation guards.
  /// </summary>
  /// <returns>Expression evaluating to <see langword="true"/> when target is empty.</returns>
  /// <remarks>
  /// Shape (string): <c>e =&gt; e.Prop != null &amp;&amp; e.Prop.Length == 0</c>
  /// Shape (collection): <c>e =&gt; e.Prop != null &amp;&amp; e.Prop.Count == 0</c>
  /// Shape (nested path): guards each nullable intermediate member before final test.
  /// </remarks>
  public override Expression<Func<T, bool>> ToExpression() {
    var param = _Selector.Parameters[0];
    var body = _Selector.Body;

    // Unbox value types (strip unary Convert) to inspect actual target type.
    if (body is UnaryExpression u && u.NodeType == ExpressionType.Convert) {
      body = u.Operand;
    }

    Expression predicate;

    if (body.Type == typeof(string)) {
      // e.Prop != null && e.Prop.Length == 0
      var notNull = Expression.NotEqual(body, Expression.Constant(null, body.Type));
      var lengthProp = Expression.Property(body, nameof(string.Length));
      var isEmpty = Expression.Equal(lengthProp, Expression.Constant(0));
      predicate = Expression.AndAlso(notNull, isEmpty);
    } else if (typeof(System.Collections.ICollection).IsAssignableFrom(body.Type)) {
      // e.Prop != null && e.Prop.Count == 0
      var notNull = Expression.NotEqual(body, Expression.Constant(null, body.Type));
      var countProp = Expression.Property(body, "Count");
      var isZero = Expression.Equal(countProp, Expression.Constant(0));
      predicate = Expression.AndAlso(notNull, isZero);
    } else {
      throw new NotSupportedException(
        "IsEmptySpecification only supports string or ICollection types.");
    }

    // Add guard chain for nested member access (null-safe navigation evaluation).
    if (body is MemberExpression me && me.Expression is not ParameterExpression) {
      predicate = GuardBuilder.Build(_Selector.Body, predicate, param);
    }
    return Expression.Lambda<Func<T, bool>>(predicate, param);
  }
}
