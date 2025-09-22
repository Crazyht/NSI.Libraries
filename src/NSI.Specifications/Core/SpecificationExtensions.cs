using System.Linq.Expressions;
using NSI.Specifications.Abstractions;

namespace NSI.Specifications.Core;

/// <summary>
/// Composition helpers (AND / OR / NOT) and raw expression conversion utilities for
/// <see cref="ISpecification{T}"/> instances with parameter unification and guard clauses.
/// </summary>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Creates new specification wrappers (immutability preserved).</description></item>
///   <item><description>Combines underlying predicate trees via <see cref="ExpressionCombiner"/>.</description></item>
///   <item><description>Does not eagerly compile expressions (deferred to consumers).</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Chain small focused specifications; avoid monolithic predicates.</description></item>
///   <item><description>Cache frequently reused composed specifications if in hot paths.</description></item>
///   <item><description>Use <see cref="ToSpecification{T}(Expression{Func{T, bool}})"/> for ad‑hoc predicates.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Each combinator allocates a single wrapper instance.</description></item>
///   <item><description>Expression tree merging cost is proportional to tree size (typically small).</description></item>
///   <item><description>No reflection or compilation performed here.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Pure stateless helpers; returned specifications should be immutable.</para>
/// </remarks>
/// <example>
/// <code>
/// var active = ((Expression&lt;Func&lt;User, bool&gt;&gt;)(u => u.Active)).ToSpecification();
/// var adult  = ((Expression&lt;Func&lt;User, bool&gt;&gt;)(u => u.Age &gt;= 18)).ToSpecification();
/// var french = ((Expression&lt;Func&lt;User, bool&gt;&gt;)(u => u.Country == "FR")).ToSpecification();
///
/// var spec = active.And(adult).And(french.Not().Not()); // double Not() just for illustration
/// var filtered = users.AsQueryable().Where(spec.ToExpression());
/// </code>
/// </example>
public static class SpecificationExtensions {
  /// <summary>
  /// Creates a specification representing logical AND of <paramref name="left"/> and
  /// <paramref name="right"/> (short-circuit semantics preserved at expression translation level).
  /// </summary>
  /// <typeparam name="T">Entity type.</typeparam>
  /// <param name="left">Left operand specification (non-null).</param>
  /// <param name="right">Right operand specification (non-null).</param>
  /// <returns>Composite specification equivalent to <c>left AND right</c>.</returns>
  /// <exception cref="ArgumentNullException">Thrown when any operand is null.</exception>
  /// <example>
  /// <code>
  /// var spec = isActiveSpec.And(isVerifiedSpec);
  /// var result = users.Where(spec.ToExpression());
  /// </code>
  /// </example>
  public static ISpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right) {
    ArgumentNullException.ThrowIfNull(left);
    ArgumentNullException.ThrowIfNull(right);
    return new ExpressionSpecification<T>(ExpressionCombiner.And(left.ToExpression(), right.ToExpression()));
  }

  /// <summary>
  /// Creates a specification representing logical OR of <paramref name="left"/> and
  /// <paramref name="right"/> (short-circuit semantics preserved).
  /// </summary>
  /// <typeparam name="T">Entity type.</typeparam>
  /// <param name="left">Left operand specification (non-null).</param>
  /// <param name="right">Right operand specification (non-null).</param>
  /// <returns>Composite specification equivalent to <c>left OR right</c>.</returns>
  /// <exception cref="ArgumentNullException">Thrown when any operand is null.</exception>
  /// <example>
  /// <code>
  /// var spec = emailConfirmedSpec.Or(phoneConfirmedSpec);
  /// var reachable = users.Where(spec.ToExpression());
  /// </code>
  /// </example>
  public static ISpecification<T> Or<T>(this ISpecification<T> left, ISpecification<T> right) {
    ArgumentNullException.ThrowIfNull(left);
    ArgumentNullException.ThrowIfNull(right);
    return new ExpressionSpecification<T>(ExpressionCombiner.Or(left.ToExpression(), right.ToExpression()));
  }

  /// <summary>
  /// Creates a specification representing logical negation of <paramref name="inner"/>.
  /// </summary>
  /// <typeparam name="T">Entity type.</typeparam>
  /// <param name="inner">Inner specification to negate (non-null).</param>
  /// <returns>Specification equivalent to <c>NOT inner</c>.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="inner"/> is null.</exception>
  /// <example>
  /// <code>
  /// var inactive = activeSpec.Not();
  /// var inactiveUsers = users.Where(inactive.ToExpression());
  /// </code>
  /// </example>
  public static ISpecification<T> Not<T>(this ISpecification<T> inner) {
    ArgumentNullException.ThrowIfNull(inner);
    return new ExpressionSpecification<T>(ExpressionCombiner.Not(inner.ToExpression()));
  }

  /// <summary>
  /// Wraps a raw predicate expression inside a specification adapter without transformation.
  /// </summary>
  /// <typeparam name="T">Entity type.</typeparam>
  /// <param name="expression">Predicate expression (non-null, provider translatable).</param>
  /// <returns>Specification exposing the supplied expression.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is null.</exception>
  /// <example>
  /// <code>
  /// var spec = ((Expression&lt;Func&lt;User, bool&gt;&gt;)(u => u.LastLoginUtc &gt;= cutoff)).ToSpecification();
  /// var recent = users.Where(spec.ToExpression());
  /// </code>
  /// </example>
  public static ISpecification<T> ToSpecification<T>(this Expression<Func<T, bool>> expression) {
    ArgumentNullException.ThrowIfNull(expression);
    return new ExpressionSpecification<T>(expression);
  }
}
